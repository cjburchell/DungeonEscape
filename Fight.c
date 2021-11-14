#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "fight.h"
#include "graphics.h"
#include "items.h"
#include "win.h"
#include "types.h"
#include "map.h"
#include "icons.h"
#define NUMBMONST  8
int monsterpic[80][80];
int ypos;
TYPE_MONSTER getmoster(int set)
{
   char files[6][20]={"plainsmt.dat",
                      "desertmt.dat",
                      "foristmt.dat",
                      "cave1mt.dat",
                      "cave2mt.dat",
                      "watermt.dat"};
   extern int monsterpic[80][80];
   int count;
   int numb;
   int hp1;
   int constpoint;
   TYPE_MONSTER monst;
   char line[100];
   FILE* data;
   numb=random(20);
   if((data=fopen(files[set],"r"))!=NULL)
   {
      for(count=0;count<=numb;count++)
      {
         fgets(line,100,data);
			sscanf(line,"%d %d %d %U %s %s %U",&hp1,&constpoint,&monst.ha,&monst.xp,monst.pic,monst.name,&monst.gold);
      }
      fclose(data);
      monst.hp=0;
      for(count=0;count<hp1;count++)
		   monst.hp=monst.hp+random(8)+1;
      monst.hp=constpoint+monst.hp;
      if(monst.hp<=0)
         monst.hp=1;
      if(getmonspic(monst.pic)==1)
      {
         gsprintfs(1,1,"unable to open %s",monst.pic);
         showdbuffer(0,0);
         get_a_key_now();
      }
   }
   else
   {
      gsprintf(1,1,"unable to open monster.dat");
      showdbuffer(0,0);
      get_a_key_now();
   }
   return(monst);
}
int attack(CPlayer *player)
{
	TYPE_MONSTER monst;
	char choice;
   int pos=14;
   int ha;
   int ma;
   enum {youd,monstd,notdead}endflag=notdead;
   switch(mapitem(player->pos[yq],player->pos[xq]))
   {
      case 3:
         player->monsttype=0;
         break;
      case 6:
         player->monsttype=1;
         break;
      case 10:
         player->monsttype=2;
         break;
      case 14:
         player->monsttype=3;
         break;
      case 22:
         player->monsttype=4;
         break;
      case 7:
         player->monsttype=5;
         break;
   }
   if(random(20)==5)
   {
      monst=getmoster(player->monsttype);
      do
      {
         fillrectangle(8,16,55*8,45*8,0);
			DisStat(player,1,2);
         if(player->hp<10)
				setcolor(RED);
			else
				setcolor(WHITE);
         Dismon(&monst,30,2);
         gsprintf(1,13,"                             ");
         gsprintf(1,14,"   1. attack                 ");
         gsprintf(1,15,"   2. run                    ");
         gsprintf(1,16,"   3. item                   ");
         gsprintf(1,17,"                             ");
			rectangle(1,13,30,18);
			rectangle(1,18,55,45);
			monstpic(37,6);
         gprintf(2,pos,"%c",16);
         showdbuffer(0,0);
         while((choice=get_a_key_now())!=SCAN_ENTER)
         {
            gprintf(2,pos," ",16);
            if(choice==72)
            {
               if(pos!=14)
                  pos--;
               else
                  pos=16;
            }
            if(choice==80)
            {
               if(pos!=16)
                  pos++;
               else
                  pos=14;
            }
            gprintf(2,pos,"%c",16);
            showdbuffer(0,0);
         }
         ypos=18;
         if(player->ha>(monst.ha*3)&&(choice==13))
				if(random(10)==1)
            {
               gsprintfs(2,++ypos,"The %s ran away",monst.name);
               showdbuffer(0,0);
               get_a_key_now();
               player->xp++;
               choice=0;
               endflag=monstd;
            }
         if(pos ==14 &&choice==SCAN_ENTER )
         {
            if(random(22-(player->agility/2))==0)
            {
               ha = (random(player->ha+(20*player->level)))+10;
               gsprintf(2,++ypos,"Heroic Manuver!");
            }
            else
				ha = (random(player->ha));
				ma = (random(monst.ha));
				ma = ma-(ma*player->defence/100);
            if(ma<0)
               ma=0;
            player->hp = player->hp - ma;
            monst.hp = monst.hp - ha;
            if(ma==0)
               gsprintf(2,++ypos,"You were unharmed");
            else
            {
               gprintf(2,++ypos,"You lost %d hit points",ma);
               flash();
            }
            if(ha==0)
               gsprintfs(2,++ypos,"The %s was unharmed",monst.name);
            else
            {
               gsiprintf(2,++ypos,"The %s has lost %d hit points",monst.name,ha);
               fillrectangle(37*8,6*8,37*8+80,6*8+80,15);
               showdbuffer(0,0);
               monstpic(37,6);
               showdbuffer(0,0);
            }
            if(player->hp>0)
            {
               if(monst.hp<=0)
               {
                  player->xp = player->xp + monst.xp;
                  player->gold=player->gold+monst.gold;
                  gsprintfs(2,++ypos,"Congratulations!! You have killed the %s",monst.name);
                  gprintf(2,++ypos,"You got %u xp",monst.xp);
                  gprintf(2,++ypos,"You got %u gold picees",monst.gold);
                  endflag=monstd;
               }
            }
            else
               endflag=youd;
            showdbuffer(0,0);
            get_a_key_now();
         }
         if(pos==15&&choice==SCAN_ENTER)
         {
            if(random(10)==1)
            {
					ma = (random(monst.ha));
					ma = ma-(ma*player->defence/100);
               if(ma<0)
                   ma=0;
               if(ma==0)
                  gsprintf(2,++ypos,"You were unharmed");
               else
               {
                  gprintf(2,++ypos,"You lost %d hit points",ma);
                  flash();
               }
               player->hp = player->hp - ma;
               if(player->hp > 0)
               {
                  if(random(5)!=1)
                  {
                     gsprintf(2,++ypos,"You got away");
                     endflag=monstd;
                  }
               }
               else
                  endflag=youd;
            }
            else
            {
               if(random(5)!=1)
               {
                  gsprintf(2,++ypos,"You got away");
                  endflag=monstd;
               }
               else
               {
				  
                  if(ma==0)
                     gsprintf(2,++ypos,"You were unharmed");
                  else
                  {
                     gprintf(2,++ypos,"You lost %d hit points",ma);
                     flash();
                  }
                  player->hp = player->hp - ma;
                  if(player->hp <= 0)
                     endflag=youd;
               }
            }
           showdbuffer(0,0);
           get_a_key_now();
         }
         if(pos==16&&choice==SCAN_ENTER)
         {
            showitems(player);
            ma = (random(monst.ha));
            ma = ma-(ma*(player->defence/100.0));
            if(ma==0)
               gsprintf(2,++ypos,"You were unharmed");
            else
            {
               gprintf(2,++ypos,"You lost %d hit points",ma);
               flash();
            }
            player->hp = player->hp - ma;
            if(player->hp <= 0)
               endflag=youd;
            showdbuffer(0,0);
            get_a_key_now();
         }
     }while(endflag==notdead);
      levelck(player);
      setscr(player->pos[xq],player->pos[yq]);
      putplayericon(player->icon,10,6,0,0);
      showdbuffer(0,0);

   }
   else
      endflag=monstd;
   return(endflag);
 }

void levelck(CPlayer* player)
{
   while(player->xp >= player->nextlevel)
   {
      gprintf(2,++ypos,"You have advanced to level %d",++player->level);
      showdbuffer(0,0);
      get_a_key_now();
      ypos=18;
		gprintf(2,++ypos,"power has incresed to %d",player->ha=player->ha+random(7));
		gprintf(2,++ypos,"hp has incresed to %d", player->maxhp=player->maxhp+random(7)+1);
		gprintf(2,++ypos,"defence has incresed to %d",player->defence=player->defence+random(4)+1);
		if (player->agility<19)
         gprintf(2,++ypos,"agility has incresed to %d",player->agility=player->agility+random(5));
      gprintf(2,++ypos,"%lu xp until the nextlevel",player->nextlevel=(player->nextlevel*2)+random(10));
      showdbuffer(0,0);
      get_a_key_now();
   }
}
void monstpic(int posx,int posy)
{
   extern int monsterpic[80][80];
	int tempx,tempy;
   posx=posx*8;
   posy=posy*8;
	for(tempx=0;tempx<80;tempx++)
		for(tempy=0;tempy<80;tempy++)
			if(monsterpic[tempy][tempx]!=16)
				putpixel(tempx+posx,tempy+posy,monsterpic[tempy][tempx]);

}
int getmonspic(char* filename)
{
   extern int monsterpic[80][80];
	FILE* iconfile;
	int tempy,tempx;
	char line[80*3+10];
	char* token;
	int temp;
	if((iconfile=fopen(filename,"r"))!=NULL)
	{
		for(tempx=0;tempx<80;tempx++)
		{
			fgets(line,300,iconfile);
			token=strtok(line," ");
			for(tempy=0;tempy<80;tempy++)
			{

				temp=atoi(token);
				monsterpic[tempx][tempy]=temp;
				token=strtok(NULL," ");
			}
		}
		fclose(iconfile);
		return(0);
   }
   else
   {
		gsprintfs(1,1,"can't open file %s",filename);
      showdbuffer(0,0);
      get_a_key_now();
		return(1);
   }
}

