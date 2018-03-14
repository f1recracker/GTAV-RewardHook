using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    // TODO unused as of now.
    // Check if props can be used
    class Utils
    {

        public static Vector3 Heading(Entity entity)
        {
            return (0.5f * entity.ForwardVector.Normalized + 0.5f * entity.Velocity.Normalized).Normalized;
        }

        // Drawing Methods

        public static void DrawBox(Vector3 a, Vector3 b, Color col)
        {
            Function.Call(Hash.DRAW_BOX, a.X, a.Y, a.Z, b.X, b.Y, b.Z, col.R, col.G, col.B, col.A);
        }

        public static void DrawBox(Vector3 a, float size, Color col)
        {
            Vector3 v1 = a - new Vector3(size, size, size);
            Vector3 v2 = a + new Vector3(size, size, size);
            DrawBox(v1, v2, col);
        }

        public static void DrawLine(Vector3 a, Vector3 b, Color col)
        {
            Function.Call(Hash.DRAW_LINE, a.X, a.Y, a.Z, b.X, b.Y, b.Z, col.R, col.G, col.B, col.A);
        }

        // Hashes

        public static List<int> TRAFFIC_SIGNAL_HASHES = new List<int> {
            0x3E2B73A4,  // prop_traffic_01a
            0x336E5E2A,  // prop_traffic_01b
            -0x271456DE, // prop_traffic_01d
            -0x2B8D60B0, // prop_traffic_02a
            0x272244B2,  // prop_traffic_02b
            0x33986EAE,  // prop_traffic_03a
            -0x5B8AE9EF  // prop_traffic_lightset_01
        };

        public static List<int> RAILWAY_SIGNAL_HASHES = new List<int>
        {
            -0x385AAA3A, // prop_traffic_rail_1a
            -0x7B1FF2E9, // prop_traffic_rail_2
            -0x152FF51   // prop_traffic_rail_3
        };
    }
}
