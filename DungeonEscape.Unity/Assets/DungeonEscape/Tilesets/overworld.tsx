<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.9" tiledversion="1.9.2" name="overworld" tilewidth="32" tileheight="32" tilecount="480" columns="30">
 <image source="images/tiles/overworld.png" trans="ff678b" width="960" height="512"/>
 <tile id="0">
  <animation>
   <frame tileid="0" duration="300"/>
   <frame tileid="1" duration="300"/>
   <frame tileid="2" duration="300"/>
  </animation>
 </tile>
 <tile id="3">
  <animation>
   <frame tileid="3" duration="300"/>
   <frame tileid="4" duration="300"/>
   <frame tileid="5" duration="300"/>
  </animation>
 </tile>
 <tile id="30">
  <animation>
   <frame tileid="30" duration="300"/>
   <frame tileid="31" duration="300"/>
   <frame tileid="32" duration="300"/>
  </animation>
 </tile>
 <tile id="33">
  <animation>
   <frame tileid="33" duration="300"/>
   <frame tileid="34" duration="300"/>
   <frame tileid="35" duration="300"/>
  </animation>
 </tile>
 <tile id="60">
  <animation>
   <frame tileid="60" duration="300"/>
   <frame tileid="61" duration="300"/>
   <frame tileid="62" duration="300"/>
  </animation>
 </tile>
 <tile id="63">
  <animation>
   <frame tileid="63" duration="300"/>
   <frame tileid="64" duration="300"/>
   <frame tileid="65" duration="300"/>
  </animation>
 </tile>
 <tile id="90">
  <animation>
   <frame tileid="90" duration="300"/>
   <frame tileid="91" duration="300"/>
   <frame tileid="92" duration="300"/>
  </animation>
 </tile>
 <tile id="93">
  <animation>
   <frame tileid="93" duration="300"/>
   <frame tileid="94" duration="300"/>
   <frame tileid="95" duration="300"/>
  </animation>
 </tile>
 <tile id="120">
  <animation>
   <frame tileid="120" duration="330"/>
   <frame tileid="121" duration="330"/>
   <frame tileid="122" duration="330"/>
  </animation>
 </tile>
 <wangsets>
  <wangset name="Snow" type="corner" tile="-1">
   <wangcolor name="Snow" color="#ff0000" tile="-1" probability="1"/>
   <wangcolor name="Grass" color="#00ff00" tile="-1" probability="1"/>
   <wangtile tileid="7" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="36" wangid="0,2,0,1,0,2,0,2"/>
   <wangtile tileid="37" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="38" wangid="0,2,0,2,0,1,0,2"/>
   <wangtile tileid="66" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="67" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="68" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="96" wangid="0,1,0,2,0,2,0,2"/>
   <wangtile tileid="97" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="98" wangid="0,2,0,2,0,2,0,1"/>
   <wangtile tileid="127" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="130" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="156" wangid="0,1,0,2,0,1,0,1"/>
   <wangtile tileid="157" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="158" wangid="0,1,0,1,0,2,0,1"/>
   <wangtile tileid="170" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="186" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="187" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="188" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="216" wangid="0,2,0,1,0,1,0,1"/>
   <wangtile tileid="217" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="218" wangid="0,1,0,1,0,1,0,2"/>
   <wangtile tileid="247" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="367" wangid="0,2,0,2,0,2,0,2"/>
  </wangset>
  <wangset name="Forest" type="corner" tile="-1">
   <wangcolor name="trees" color="#ff0000" tile="-1" probability="1"/>
   <wangcolor name="void" color="#00ff00" tile="-1" probability="1"/>
   <wangtile tileid="295" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="325" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="355" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="387" wangid="0,1,0,1,0,1,0,2"/>
   <wangtile tileid="388" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="389" wangid="0,2,0,1,0,1,0,1"/>
   <wangtile tileid="416" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="417" wangid="0,2,0,1,0,2,0,2"/>
   <wangtile tileid="418" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="419" wangid="0,2,0,2,0,1,0,2"/>
   <wangtile tileid="447" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="448" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="449" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="477" wangid="0,1,0,2,0,2,0,2"/>
   <wangtile tileid="478" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="479" wangid="0,2,0,2,0,2,0,1"/>
  </wangset>
  <wangset name="Swamp" type="corner" tile="-1">
   <wangcolor name="swamp" color="#ff0000" tile="-1" probability="1"/>
   <wangcolor name="Grass" color="#00ff00" tile="-1" probability="1"/>
   <wangtile tileid="7" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="170" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="187" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="247" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="367" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="396" wangid="0,2,0,1,0,2,0,2"/>
   <wangtile tileid="397" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="398" wangid="0,2,0,2,0,1,0,2"/>
   <wangtile tileid="426" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="427" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="428" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="456" wangid="0,1,0,2,0,2,0,2"/>
   <wangtile tileid="457" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="458" wangid="0,2,0,2,0,2,0,1"/>
  </wangset>
  <wangset name="Desert" type="corner" tile="-1">
   <wangcolor name="sand" color="#ff0000" tile="-1" probability="1"/>
   <wangcolor name="grass" color="#00ff00" tile="-1" probability="1"/>
   <wangtile tileid="7" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="130" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="170" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="247" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="276" wangid="0,2,0,1,0,2,0,2"/>
   <wangtile tileid="277" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="278" wangid="0,2,0,2,0,1,0,2"/>
   <wangtile tileid="306" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="307" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="308" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="336" wangid="0,1,0,2,0,2,0,2"/>
   <wangtile tileid="337" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="338" wangid="0,2,0,2,0,2,0,1"/>
   <wangtile tileid="367" wangid="0,2,0,2,0,2,0,2"/>
  </wangset>
  <wangset name="Hills" type="corner" tile="-1">
   <wangcolor name="hills" color="#ff0000" tile="-1" probability="1"/>
   <wangcolor name="void" color="#00ff00" tile="-1" probability="1"/>
   <wangtile tileid="295" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="296" wangid="0,2,0,1,0,2,0,2"/>
   <wangtile tileid="297" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="298" wangid="0,2,0,1,0,1,0,2"/>
   <wangtile tileid="299" wangid="0,2,0,2,0,1,0,2"/>
   <wangtile tileid="325" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="326" wangid="0,1,0,1,0,2,0,2"/>
   <wangtile tileid="327" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="328" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="329" wangid="0,2,0,2,0,1,0,1"/>
   <wangtile tileid="355" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="356" wangid="0,1,0,2,0,2,0,2"/>
   <wangtile tileid="357" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="358" wangid="0,1,0,2,0,2,0,1"/>
   <wangtile tileid="359" wangid="0,2,0,2,0,2,0,1"/>
   <wangtile tileid="416" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="446" wangid="0,2,0,2,0,2,0,2"/>
   <wangtile tileid="476" wangid="0,2,0,2,0,2,0,2"/>
  </wangset>
 </wangsets>
</tileset>
