#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Models
{
    /// <summary>
    /// En spelbar karta byggd från en LevelObj — används vid preview (F5) och
    /// My Maps-spelning. Spawnar portal, pickups och fiender ur LevelObj.Objects.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Template Method (specialisering av Map)
    ///
    /// MOTIVERING:
    /// Spelkartor (MapOne–MapNine) har hårdkodade PopulateDynamics-metoder.
    /// Editorkartor vet inte sina objekt förrän i runtime — de läses ur
    /// LevelObj.Objects (Tiled JSON Objects-lager). UserMap bridgar
    /// det dynamiska editorformatet till spelets fasta Map-arkitektur utan
    /// att ändra GameplayState eller Program.ChangeMap.
    ///
    /// EnemyFactory och ItemFactory skapas internt eftersom de är tillståndslösa.
    ///
    /// ANVÄNDNING:
    /// Skapas av EditorState.HandlePreviewPlay() vid F5-tryck.
    /// Konstruktorn tar LevelObj + IAssets; fabriker skapas internt.
    /// context.CurrentLevel sätts till den skapade instansen innan
    /// övergång till GameplayState.
    /// </remarks>
    public class UserMap : Map
    {
        private readonly LevelObj _level;

        /// <summary>
        /// Skapar en spelbar Map från en editorladdad LevelObj.
        /// </summary>
        /// <param name="level">Kartdata — tiles, kollision och placerade objekt.</param>
        /// <param name="mapName">Kartans namn (t.ex. "slot1"). Måste skilja sig från "worldmap".</param>
        /// <param name="assets">Speltillgångar — sprites och items.</param>
        public UserMap(LevelObj level, string mapName, IAssets assets)
        {
            Assets       = assets;
            EnemyFactory = new EnemyFactory();
            ItemFactory  = new ItemFactory();
            _level       = level;

            string  assetName  = level.TilesetSource.Replace(".tsx", "");
            string? spritePath = assets.GetSpritePath(assetName);

            Create(new CreateObj
            {
                levelObj   = level,
                spritePath = spritePath,
                name       = mapName,
            });
        }

        /// <summary>
        /// Spawnar portal, pickups och fiender ur LevelObj.Objects.
        /// Fiende-ID:n startar på 1000 för att inte krocka med inbyggda kartors ID:n.
        /// </summary>
        public override bool PopulateDynamics(List<DynamicGameObject> list)
        {
            int nextId = 1000;

            foreach (var obj in _level.Objects)
            {
                switch (obj.ObjectType)
                {
                    case "Goal":
                        list.Add(new Teleport(obj.TileX, obj.TileY, MapName.WorldMap, 2f, 3f));
                        break;

                    case "Pickup" when obj.SubType == "Energy":
                        list.Add(ItemFactory!.Create(ItemType.Energi, obj.TileX, obj.TileY, Assets!, id: nextId++));
                        break;

                    case "Enemy":
                        EnemyType? type = SubTypeToEnemyType(obj.SubType);
                        if (type.HasValue)
                        {
                            var enemy = EnemyFactory!.Create(type.Value, Assets!);
                            enemy.px   = obj.TileX;
                            enemy.py   = obj.TileY;
                            enemy.Name = obj.SubType.ToLower();
                            list.Add(enemy);
                        }
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Triggar teleport-kommando när spelaren rör portalen.
        /// I preview-läge fångas den resulterande worldmap-transitionn av GameplayState.
        /// </summary>
        public override bool OnInteraction(List<DynamicGameObject> list, DynamicGameObject target, Enum.NATURE nature)
        {
            if (target.Name == "Teleport")
                Script.AddCommand(new CommandChangeMap(
                    ((Teleport)target).MapName,
                    ((Teleport)target).MapPosX,
                    ((Teleport)target).MapPosY));
            return false;
        }

        private static EnemyType? SubTypeToEnemyType(string subType) => subType switch
        {
            "Penguin" => EnemyType.Penguin,
            "Walrus"  => EnemyType.Walrus,
            "Frost"   => EnemyType.Frost,
            _         => (EnemyType?)null,
        };
    }
}
