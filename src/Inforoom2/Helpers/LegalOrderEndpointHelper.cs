using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Inforoom2.Models;
using NHibernate;

namespace Inforoom2.Helpers
{
	public class LegalOrderEndpointHelper
	{
		public enum ChangesState
		{
			[Description("Действий нет")] NoChanges = 0,
			[Description("Добавление текущего Endpoint(а)")] CurrentEndpointInsert,
			[Description("Изменение текущего не активного Endpoint(а)")] CurrentEndpointUpdateDisabled,
			[Description("Создание будущего Endpoint(а) для полной замены текущего")] CurrentEndpointUpdateFull,
			[Description("Добавление состояния Endpoint(а) для редактирования текущего активного Endpoint(а)")] CurrentEndpointUpdatePartial,
			[Description("Изменение будущего состояния текущего Endpoint(а) для полной замены текущего")] FutureStateUpdateFull,
			[Description("Изменение будущего состояния текущего активного Endpoint(а) для его редактирования")] FutureStateUpdatePartial,
			[Description("Изменение будущего Endpoint(а) - *он всегда неактивен, поэтому состояния у него нет")] FutureEndpointUpdate,
			[Description("Смена эндпоинта")] ChangeEndpoints
		}

		public enum Difference
		{
			NoDifference,
			SmallDiffecence,
			BigDIfference
		}

		public static Difference AreEndpointAndConnectionSettingsDifference(ClientEndpoint currentEndpoint, ConnectionHelper connection)
		{
			int port = 0;
			int.TryParse(connection.Port ?? "0", out port);
			if (currentEndpoint.Switch?.Id != connection.Switch || currentEndpoint.Port != port) {
				return Difference.BigDIfference;
			}
			if (connection.Monitoring != currentEndpoint.Monitoring)
				return Difference.SmallDiffecence;
			if ((connection.PackageId != 0 && currentEndpoint.PackageId == null) || (currentEndpoint.PackageId.HasValue && connection.PackageId != currentEndpoint.PackageId.Value))
				return Difference.SmallDiffecence;
			if (connection.Pool != currentEndpoint.Pool?.Id)
				return Difference.SmallDiffecence;
			if (port != currentEndpoint.Port)
				return Difference.SmallDiffecence;
			if (!(connection.StaticIp == null && currentEndpoint.Ip == null) && connection.StaticIp != (currentEndpoint.Ip != null ? currentEndpoint.Ip.ToString() : ""))
				return Difference.SmallDiffecence;

			return Difference.NoDifference;
		}


		public static ChangesState CheckChangesState(ClientOrder currentOrder, ClientEndpoint currentEndpoint, ConnectionHelper connection)
		{
			var EndPointFutureStateExists = currentOrder.HasEndPointFutureState;
			var currentEndpointResult = currentEndpoint != null ? AreEndpointAndConnectionSettingsDifference(currentEndpoint, connection) : Difference.NoDifference;

			//заказ без endpoit, добавление endpoit
			if (((currentEndpoint == null || currentEndpoint.Id == 0)
			     && !EndPointFutureStateExists)
			    && (currentEndpointResult == Difference.BigDIfference)) {
				return ChangesState.CurrentEndpointInsert;
			}
			//заказ с неактивированным endpoit, обновление endpoit
			if (((currentEndpoint != null && currentEndpoint.Id != 0) && currentEndpoint.IsEnabled == null
			     && !EndPointFutureStateExists)
			    && (currentEndpointResult != Difference.NoDifference)) {
				return ChangesState.CurrentEndpointUpdateDisabled;
			}
			//заказ с активированным endpoit, добавление endpoitState
			if (((currentEndpoint != null && currentEndpoint.Id != 0) && currentEndpoint.IsEnabled != null
			     && !EndPointFutureStateExists)
			    && (currentEndpointResult != Difference.NoDifference)) {
				if (currentEndpointResult == Difference.BigDIfference) {
					return ChangesState.CurrentEndpointUpdateFull;
				}
				if (currentEndpointResult == Difference.SmallDiffecence) {
					return ChangesState.CurrentEndpointUpdatePartial;
				}
			}
			//заказ с активированным endpoit, изменение endpoitState (только состояния)
			if (((currentEndpoint != null && currentEndpoint.Id != 0) && currentEndpoint.IsEnabled != null
			     && EndPointFutureStateExists)
			    && currentEndpointResult != Difference.NoDifference) {
				if (currentEndpointResult == Difference.SmallDiffecence)
					return ChangesState.FutureStateUpdatePartial;
				else
					return ChangesState.FutureStateUpdateFull;
			}
			//заказ с активированным endpoit, изменение endpoitState (изменение будущего endpoit(а))
			if (((currentEndpoint != null && currentEndpoint.Id != 0)
			     && EndPointFutureStateExists)
			    && (currentEndpointResult != Difference.NoDifference)) {
				return ChangesState.FutureEndpointUpdate;
			}
			//заказ с не активированным endpoit, изменение endpoitState (изменение будущего endpoit(а))
			if (((currentEndpoint != null || currentEndpoint.Id != 0)
			     && EndPointFutureStateExists)
			    && (currentEndpointResult == Difference.BigDIfference)) {
				return ChangesState.FutureEndpointUpdate;
			}
			return ChangesState.NoChanges;
		}
	}
}