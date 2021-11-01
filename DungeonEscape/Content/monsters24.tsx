<?xml version="1.0" encoding="utf-8"?>
<tileset xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="Monsters 24" tilewidth="32" tileheight="32" tilecount="2" columns="0" spacing="0" margin="0" transparentcolor="#FF00FF" firstgid="1">
  <tile id="90" type="Goblin">
    <image width="32" height="32" source="images/monsters/gob.png" />
    <properties>
      <property name="Biome" type="string" value="All" />
      <property name="Health" type="int" value="1" />
      <property name="HealthConst" type="int" value="0" />
      <property name="Attack" type="int" value="9" />
      <property name="XP" type="int" value="5" />
      <property name="Gold" type="int" value="5" />
      <property name="Agility" type="int" value="5" />
      <property name="Defence" type="int" value="5" />
      <property name="Chance" type="int" value="7" />
      <property name="MinLevel" type="int" value="1" />
    </properties>
  </tile>
  <tile id="91" type="Hobgoblin">
    <image width="32" height="32" source="images/monsters/hgob.png" />
    <properties>
      <property name="Biome" type="string" value="All" />
      <property name="Health" type="int" value="2" />
      <property name="HealthConst" type="int" value="1" />
      <property name="Attack" type="int" value="9" />
      <property name="XP" type="int" value="15" />
      <property name="Gold" type="int" value="22" />
      <property name="Agility" type="int" value="5" />
      <property name="Defence" type="int" value="5" />
      <property name="Chance" type="int" value="6" />
      <property name="MinLevel" type="int" value="1" />
    </properties>
  </tile>
</tileset>