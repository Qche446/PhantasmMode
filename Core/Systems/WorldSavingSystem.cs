using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Core.Systems
{
    public class WorldSavingSystem : ModSystem
    {
        public static bool PhantasmMode { get; set; }
        private static void ResetFlags()
        {
            PhantasmMode = false;
        }
        public override void OnWorldLoad() => ResetFlags();

        public override void OnWorldUnload() => ResetFlags();
        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = [];
            if (PhantasmMode)
                downed.Add("phantasm");
            tag.Add("downed", downed);
        }
        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");
            PhantasmMode = downed.Contains("phantasm");
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(PhantasmMode);
        }
        public override void NetReceive(BinaryReader reader)
        {
            PhantasmMode = reader.ReadBoolean();
        }
    }
}
