<?xml version="1.0" encoding="utf-8"?>
<tileset xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="spells" tilewidth="32" tileheight="32" tilecount="6" columns="0" spacing="0" margin="0" transparentcolor="#FF00FF" firstgid="1">
  <tile id="1" type="Heal">
    <image width="32" height="32" source="images/items/healspell.png" />
    <properties>
      <property name="Name" type="string" value="Heal" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="5" />
      <property name="MinLevel" type="int" value="2" />
    </properties>
  </tile>
  <tile id="2" type="Outside">
    <image width="32" height="32" source="images/items/outside.png" />
    <properties>
      <property name="Name" type="string" value="Outside" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="4" />
    </properties>
  </tile>
  <tile id="3" type="Fireball">
    <image width="32" height="32" source="images/items/fireball.png" />
    <properties>
      <property name="Name" type="string" value="FireBall" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="6" />
    </properties>
  </tile>
  <tile id="4" type="Lighting">
    <image width="32" height="32" source="images/items/Lightning.png" />
    <properties>
      <property name="Name" type="string" value="LitBall" />
      <property name="Power" type="int" value="1" />
      <property name="Cost" type="int" value="15" />
      <property name="MinLevel" type="int" value="8" />
    </properties>
  </tile>
  <tile id="5" type="Fireball">
    <image width="32" height="32" source="images/items/FireBlast.png" />
    <properties>
      <property name="Name" type="string" value="FireBlast" />
      <property name="Power" type="int" value="2" />
      <property name="Cost" type="int" value="25" />
      <property name="MinLevel" type="int" value="10" />
    </properties>
  </tile>
  <tile id="6" type="Lighting">
    <image width="32" height="32" source="images/items/LitBlast.png" />
    <properties>
      <property name="Name" type="string" value="LitBlast" />
      <property name="Power" type="int" value="2" />
      <property name="Cost" type="int" value="30" />
      <property name="MinLevel" type="int" value="12" />
    </properties>
  </tile>
</tileset>