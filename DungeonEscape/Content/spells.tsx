<?xml version="1.0" encoding="utf-8"?>
<tileset xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="spells" tilewidth="32" tileheight="32" tilecount="6" columns="0" spacing="0" margin="0" transparentcolor="#FF00FF" firstgid="1">
  <tile id="45" type="Heal">
    <image width="32" height="32" source="images/items/healspell.bmp" />
    <properties>
      <property name="Name" type="string" value="Heal" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="5" />
      <property name="MinLevel" type="int" value="2" />
    </properties>
  </tile>
  <tile id="46" type="Outside">
    <image width="32" height="32" source="images/items/outside.bmp" />
    <properties>
      <property name="Name" type="string" value="Outside" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="4" />
    </properties>
  </tile>
  <tile id="47" type="Fireball">
    <image width="32" height="32" source="images/items/fireball.bmp" />
    <properties>
      <property name="Name" type="string" value="FireBall" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="6" />
    </properties>
  </tile>
  <tile id="48" type="Lighting">
    <image width="32" height="32" source="images/items/Lightning.bmp" />
    <properties>
      <property name="Name" type="string" value="LitBall" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="15" />
      <property name="MinLevel" type="int" value="8" />
    </properties>
  </tile>
  <tile id="49" type="Fireball">
    <image width="32" height="32" source="images/items/FireBlast.bmp" />
    <properties>
      <property name="Name" type="string" value="FireBlast" />
      <property name="Power" type="int" value="2" />
      <property name="Cost" type="int" value="25" />
      <property name="MinLevel" type="int" value="10" />
    </properties>
  </tile>
  <tile id="50" type="Lighting">
    <image width="32" height="32" source="images/items/LitBlast.bmp" />
    <properties>
      <property name="Name" type="string" value="LitBlast" />
      <property name="Power" type="int" value="2" />
      <property name="Cost" type="int" value="30" />
      <property name="MinLevel" type="int" value="12" />
    </properties>
  </tile>
</tileset>