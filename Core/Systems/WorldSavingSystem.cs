using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Core.Systems
{
    public class PModeWorldSavingSystem : ModSystem
    {
        public static bool PhantasmMode { get; set; }
        public static bool CanPlayPhantasm {  get; set; }
        private static void ResetFlags()
        {
            CanPlayPhantasm = false;
            PhantasmMode = false;
        }
        public override void OnWorldLoad() => ResetFlags();

        public override void OnWorldUnload() => ResetFlags();
        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = [];
            if (CanPlayPhantasm)
                downed.Add("CanPlayPhantasm");
            if (PhantasmMode)
                downed.Add("phantasm");
            tag.Add("downed", downed);
        }
        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");
            CanPlayPhantasm = downed.Contains("CanPlayPhantasm");
            PhantasmMode = downed.Contains("phantasm");
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(CanPlayPhantasm);
            writer.Write(PhantasmMode);
        }
        public override void NetReceive(BinaryReader reader)
        {
            CanPlayPhantasm = reader.ReadBoolean();
            PhantasmMode = reader.ReadBoolean();
        }
    }
}
