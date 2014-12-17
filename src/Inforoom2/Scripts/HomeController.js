/**
* Слайдер картинок
*/
function Slider()
{
	/**
	* Сколько по времени пользователь видит один слайд в милисекунах
	*/
	this.slideTimeout = 2000;
	/**
	* Скорость анимации в милисекунах
	*/
	this.animationSpeed = 1000;

  this.initialized = false;
  this.stopFlag = false;
  var timeout = null;
  this.count = $(".slider .offer").length;
  var img  =  $(".slider .offer")[0];
  var width = parseInt($(img).css("width"));
  var margin = parseInt($(img).css("margin-left"));
  this.iteration = 0;
  this.animation = null;
  
  this.start = function()
  {
     if(!this.initialized)
        this.init();
     setTimeout(this.move.bind(this), this.slideTimeout);
  }
  
  this.init = function()
  {
      console.log("Initializing slider");
      this.initialized = true;
      
      var content =  $('.slider .longspace').html();
      $('.slider .longspace').html(content+content);
      img = $(".slider .offer")[0];
      $(".slider li").each(function(i,el){
        $(el).on("click",function(){
            console.log('move',i)
            this.release();
            this.move(i);           
        }.bind(this));
      }.bind(this))
      
      var stopButton = $(".slider .stop");
      stopButton.off("click");
      stopButton.on("click",function(){
        console.log("stopping",this.stopFlag)
        if(!this.stopFlag)
        {
            stopButton.addClass("active");
            this.stop();
        }
        else
        {
            stopButton.removeClass("active");
            this.release();
            setTimeout(this.move.bind(this), this.slideTimeout);
        }
        
      }.bind(this));
      return;
  }  
  this.stop = function()
  { 
      console.log("Stoping slider");
      clearTimeout(timeout);
      
      this.stopFlag = true;
      return;
  }
  this.release = function()
  {
      console.log("Releasing slider")
      this.stopFlag = false;
  }
  this.move = function(iteration)
  { 
         
    //если нажата клавиша стоп
    if(this.stopFlag)
        return;
    
    this.iteration++;
    if(iteration != null)
    {
        this.stop();
        this.release();
        $(img).stop();
        this.iteration = iteration;
    }
    
    console.log("Move slider: ",'width',width,'margin',margin,'iteration',this.iteration)

    //активность маленьких иконок
    $(".slider li").removeClass("active");
    var bullet = $(".slider li").get(this.iteration+1 > this.count ? 0 : this.iteration );
      $(bullet).addClass("active");

    //анимация
    console.log(img);
    this.animation = $(img).animate({marginLeft: -width*this.iteration},this.animationSpeed,null,function(){
       if(this.count == this.iteration)
       {  
           console.log("Reseting slider");
           $(img).css("margin-left","0px");
           this.iteration = 0;
       }
       if(!this.stopFlag)
			timeout = setTimeout(this.move.bind(this), this.slideTimeout);
       else
          console.log("Stoping iterations",iteration);
    }.bind(this));
  }
  
}

var slider =  new Slider();
slider.start(slider);
