using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyPlatformer.Systems;

// Exempel

// Detta är ett exempel på hur vi ska undvika att använda magiska strängar i sln
public static class SystemDictionary
{

    /*
     Vi ska inte ha magisak strängar i projektet. 
     Alla strängvärden som används i projektet ska använda denna statiska klass (SystemDictionary) och sedan rätt subklass.
     Alla strängvärden ska samlas i "kohort"/kategorier med sin egna subklass. Namn på kartor ligger i en klass, namn på ljudfiler ligger i en klass, osv.
     */

    public static class Map
    {
        const string WorldMap = "worldmap";
    }

    public static class  Exstension
    {
        const string png = "png";
        const string dotPng = "."+ png;
    }

}
