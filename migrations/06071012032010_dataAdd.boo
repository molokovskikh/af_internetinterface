import System
import System.Collections.Generic
import System.Linq.Enumerable from System.Core
import Castle.DynamicProxy from Castle.DynamicProxy2

#import Castle.ActiveRecord from "src/Lib/AR 2.1.2/Castle.ActiveRecord.dll"
import InternetInterface.Models

Accesscat = AccessCategories()
Accesscat.Name = "Получить информацию по клиентам"
Accesscat.ReduceName = "GCI"
Accesscat.SaveAndFlush()
Accesscat = AccessCategories()
Accesscat.Name = "Зарегистрировать нового клиента"
Accesscat.ReduceName = "RC"
Accesscat.SaveAndFlush()
Accesscat = AccessCategories()
Accesscat.Name = "Отправить заявки бригадиру бригад"
Accesscat.ReduceName = "SD"
Accesscat.SaveAndFlush()
Accesscat = AccessCategories()
Accesscat.Name = "Закрыть заявки"
Accesscat.ReduceName = "CD"
Accesscat.SaveAndFlush()
Accesscat = AccessCategories()
Accesscat.Name = "Регистрировать партнеров"
Accesscat.ReduceName = "RP"
Accesscat.SaveAndFlush()
Accesscat = AccessCategories()
Accesscat.Name = "Пополнение баланса клиента"
Accesscat.ReduceName = "CB"
Accesscat.SaveAndFlush()