<?xml version="1.0" encoding="utf-8"?>
<tileset xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="spells" tilewidth="32" tileheight="32" tilecount="6" columns="0" spacing="0" margin="0" transparentcolor="#FF00FF" firstgid="1">
  <tile id="1" type="Heal">
    <image width="32" height="32" source="images/items/healspell.png" />
    <properties>
      <property name="Name" type="string" value="Heal" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="3" />
      <property name="MinLevel" type="int" value="2" />
      <property name="Health" type="int" value="10" />
      <property name="HealthConst" type="int" value="40" />
    </properties>
  </tile>
  <tile id="2" type="Outside">
    <image width="32" height="32" source="images/items/outside.png" />
    <properties>
      <property name="Name" type="string" value="Outside" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="6" />
      <property name="MinLevel" type="int" value="12" />
    </properties>
  </tile>
  <tile id="3" type="Damage">
    <image width="32" height="32" source="images/items/fireball.png" />
    <properties>
      <property name="Name" type="string" value="Fire Ball" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="6" />
      <property name="Health" type="int" value="10" />
      <property name="HealthConst" type="int" value="5" />
    </properties>
  </tile>
  <tile id="4" type="Damage">
    <image width="32" height="32" source="images/items/Lightning.png" />
    <properties>
      <property name="Name" type="string" value="Lightning" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="15" />
      <property name="MinLevel" type="int" value="8" />
      <property name="Health" type="int" value="20" />
      <property name="HealthConst" type="int" value="5" />
    </properties>
  </tile>
  <tile id="5" type="Damage">
    <image width="32" height="32" source="images/items/FireBlast.png" />
    <properties>
      <property name="Name" type="string" value="Fire Blast" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="25" />
      <property name="MinLevel" type="int" value="10" />
      <property name="Health" type="int" value="10" />
      <property name="HealthConst" type="int" value="5" />
    </properties>
  </tile>
  <tile id="6" type="Damage">
    <image width="32" height="32" source="images/items/LitBlast.png" />
    <properties>
      <property name="Name" type="string" value="Lightning Storm" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="30" />
      <property name="MinLevel" type="int" value="12" />
      <property name="Health" type="int" value="20" />
      <property name="HealthConst" type="int" value="5" />
    </properties>
  </tile>
  <tile id="7" type="Return">
    <image width="32" height="32" source="images/items/return.png" />
    <properties>
      <property name="Name" type="string" value="Return" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="6" />
      <property name="MinLevel" type="int" value="12" />
    </properties>
  </tile>
  <tile id="8" type="Revive">
    <image width="32" height="32" source="images/items/revive.png" />
    <properties>
      <property name="Name" type="string" value="Revive" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="25" />
      <property name="MinLevel" type="int" value="25" />
      <property name="Health" type="int" value="0" />
      <property name="HealthConst" type="int" value="1" />
    </properties>
  </tile>
  <tile id="8" type="Revive">
    <image width="32" height="32" source="images/items/revive.png" />
    <properties>
      <property name="Name" type="string" value="Vivify" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="10" />
      <property name="MinLevel" type="int" value="25" />
    </properties>
  </tile>
  <tile id="9" type="Heal">
    <image width="32" height="32" source="images/items/healspell.png" />
    <properties>
      <property name="Name" type="string" value="Heal All" />
      <property name="Targets" type="string" value="Single" />
      <property name="Cost" type="int" value="7" />
      <property name="MinLevel" type="int" value="30" />
    </properties>
  </tile>
  <tile id="10" type="Heal">
    <image width="32" height="32" source="images/items/healspell.png" />
    <properties>
      <property name="Name" type="string" value="Heal Us" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="18" />
      <property name="MinLevel" type="int" value="34" />
      <property name="Health" type="int" value="10" />
      <property name="HealthConst" type="int" value="40" />
    </properties>
  </tile>
  <tile id="11" type="Heal">
    <image width="32" height="32" source="images/items/healspell.png" />
    <properties>
      <property name="Name" type="string" value="Heal Us All" />
      <property name="Targets" type="string" value="Group" />
      <property name="Cost" type="int" value="62" />
      <property name="MinLevel" type="int" value="38" />
    </properties>
  </tile>
</tileset>