using System;

using MCGalaxy.Drawing;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Network;

using BlockID = System.UInt16;

namespace MCGalaxy
{
    public class MoveSelection : Plugin
    {
        public override string name { get { return "MoveSelection"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdMoveSelection());
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("MoveSelection"));
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect); // Initialize hotkeys when the player joins the server, if they support them.
        }

        private void HandlePlayerConnect(Player p)
        {
            if (p.Supports(CpeExt.TextHotkey) && p.hasCP437)
            {
                /* Hotkeys to make the selection process easier. The '◙' character makes the client automatically send the hotkey instead
					of just typing it into the chat box. */
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection north◙", 72, 0, true)); // NUM_PAD_8.
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection east◙", 77, 0, true)); // NUM_PAD_6.
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection south◙", 80, 0, true)); // NUM_PAD_2.
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection west◙", 75, 0, true)); // NUM_PAD_4.
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection up◙", 78, 0, true)); // NUM_PAD_PLUS.
                p.Send(Packet.TextHotKey("MoveSelection", "/MoveSelection down◙", 74, 0, true)); // NUM_PAD_MINUS.
            }
        }
    }

    public sealed class CmdMoveSelection : Command2
    {
        public override string name { get { return "MoveSelection"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }

        public override void Help(Player p)
        {
            p.Message("&T/MoveSelection <size>");
            p.Message("&HSelects all blocks between two points.");
            p.Message("&HIf <size> is specified, movements will increment by this number.");
            p.Message("&T/MoveSelection [n/e/s/w/up/down]");
            p.Message("&HMoves the selection.");
        }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0)
            {
                p.Message("Place or break two blocks to determine the selection area.");
                p.MakeSelection(2, "Selecting region for &SMoveSelection", null, CreateSelection);
                return;
            }

            string[] args = message.SplitSpaces();
            string[] directions = new string[] { "north", "east", "south", "west", "up", "down" };

            if (Array.Exists(directions, element => element.CaselessEq(args[0])))
            {
                MoveSelection(p, args[0]);
                return;
            }

            int increment = 1;
            int.TryParse(args[0], out increment);

            if (increment < 1 || increment > 16)
            {
                p.Message("Please use an increment between 1-16.");
                return;
            }

            p.Message("Place or break two blocks to determine the selection area.");
            p.Extras["MOVESELECTION_INCREMENT"] = increment;
            p.MakeSelection(2, "Selecting region for &SMoveSelection", null, CreateSelection);
        }

        private int[] GetCoordinateChangeFromDirection(Player p, string direction)
        {
            int increment = p.Extras.Get("MOVESELECTION_INCREMENT") != null ? p.Extras.GetInt("MOVESELECTION_INCREMENT") : 1;

            switch (direction.ToLower())
            {
                case "north": return new int[] { 0, 0, -1 * increment };
                case "east": return new int[] { 1 * increment, 0, 0 };
                case "south": return new int[] { 0, 0, 1 * increment };
                case "west": return new int[] { -1 * increment, 0, 0 };
                case "up": return new int[] { 0, 1 * increment, 0 };
                case "down": return new int[] { 0, -1 * increment, 0 };
                default: return new int[] { 0, 0, 0 }; // Default to no movement.
            }
        }

        private Vec3U16 RotateVector(int[] vector, float yaw)
        {
            float radians = (float)(yaw * (Math.PI / 180.0)); // Player yaw is in degrees.
            float cosAngle = (float)Math.Cos(radians);
            float sinAngle = (float)Math.Sin(radians);

            // Only rotate X and Z, since Y is unaffected by player yaw.
            ushort xNew = (ushort)Math.Round(vector[0] * cosAngle - vector[2] * sinAngle);
            ushort zNew = (ushort)Math.Round(vector[0] * sinAngle + vector[2] * cosAngle);

            return new Vec3U16(xNew, (ushort)vector[1], zNew);
        }

        private void MoveSelection(Player p, string input)
        {
            // Input collected from the command/hotkey. E.g, "north", "east", "up".
            int[] inputDirection = GetCoordinateChangeFromDirection(p, input);
            // For easability, we can adjust the direction based on the direction the player is looking at. E.g, north input and looking east = east.
            Vec3U16 rotatedDirection = RotateVector(inputDirection, Orientation.PackedToDegrees(p.Rot.RotY));

            Vec3U16 min = (Vec3U16)p.Extras.Get("MOVESELECTION_MIN");
            Vec3U16 max = (Vec3U16)p.Extras.Get("MOVESELECTION_MAX");

            Vec3U16 newMin = new Vec3U16((ushort)(min.X + rotatedDirection.X), (ushort)(min.Y + rotatedDirection.Y), (ushort)(min.Z + rotatedDirection.Z));
            Vec3U16 newMax = new Vec3U16((ushort)(max.X + rotatedDirection.X), (ushort)(max.Y + rotatedDirection.Y), (ushort)(max.Z + rotatedDirection.Z));

            if (!p.level.IsValidPos(newMin.X, newMin.Y, newMin.Z) || !p.level.IsValidPos(newMax.X, newMax.Y, newMax.Z))
            {
                p.Message("&cEdge of map detected.");
                return;
            }

            // Remove all blocks.

            DrawOp op = new CuboidDrawOp();

            //op.Flags = BlockDBFlags.Cut;
            op.AffectedByTransform = false;
            Brush brush = new SolidBrush(Block.Air);
            DrawOpPerformer.Do(op, brush, p, new Vec3S32[] { min, max }, false);

            PasteSelection(p, newMin, newMax);

            p.Extras["MOVESELECTION_MIN"] = newMin;
            p.Extras["MOVESELECTION_MAX"] = newMax;

            p.Send(Packet.DeleteSelection(255)); // Delete the last zone marker in order to create new one with same ID.

            // Create a new zone marker.
            ColorDesc col; Colors.TryParseHex("5ECAD1", out col);
            p.Send(Packet.MakeSelection(255, "Boundary", newMin, new Vec3U16((ushort)(newMax.X + 1), (ushort)(newMax.Y + 1), (ushort)(newMax.Z + 1)), col.R, col.G, col.B, 128, p.hasCP437));
        }

        private void PasteSelection(Player p, Vec3U16 min, Vec3U16 max)
        {
            // Paste the selection.
            BrushArgs args = new BrushArgs(p, "", Block.Air);
            Brush brush = BrushFactory.Find("Paste").Construct(args);
            if (brush == null) return;

            Vec3S32[] marks = new[] { new Vec3S32(min.X, min.Y, min.Z) };

            CopyState cState = p.CurrentCopy;
            PasteDrawOp op = new PasteDrawOp();
            op.CopyState = cState;

            //marks[0] += cState.Offset;

            DrawOpPerformer.Do(op, brush, p, marks);
        }

        private bool CreateSelection(Player p, Vec3S32[] marks, object state, BlockID block)
        {
            if (!p.Supports(CpeExt.SelectionCuboid))
            {
                p.Message("&SYour client needs to support &bSelectionCuboid &Sin order to use this command.");
                return false;
            }

            Vec3U16 min = (Vec3U16)marks[0];
            Vec3U16 max = (Vec3U16)marks[1];

            Vec3U16 newMin = new Vec3U16(
                Math.Min(min.X, max.X),
                Math.Min(min.Y, max.Y),
                Math.Min(min.Z, max.Z)
            );

            Vec3U16 newMax = new Vec3U16(
                Math.Max(min.X, max.X),
                Math.Max(min.Y, max.Y),
                Math.Max(min.Z, max.Z)
            );

            // Create a translucent zone around the selection so it can be seen easily.
            ColorDesc col; Colors.TryParseHex("5ECAD1", out col);
            p.Send(Packet.MakeSelection(255, "Boundary", newMin, new Vec3U16((ushort)(newMax.X + 1), (ushort)(newMax.Y + 1), (ushort)(newMax.Z + 1)), col.R, col.G, col.B, 128, p.hasCP437));

            // Initialize the marks so we can reference them when we move the selection.
            p.Extras["MOVESELECTION_MIN"] = newMin;
            p.Extras["MOVESELECTION_MAX"] = newMax;

            ushort width = (ushort)(newMax.X - newMin.X + 1);
            ushort height = (ushort)(newMax.Y - newMin.Y + 1);
            ushort length = (ushort)(newMax.Z - newMin.Z + 1);

            CopyState cState = new CopyState(newMin.X, newMin.Y, newMin.Z, width, height, length);
            cState.OriginX = newMin.X; cState.OriginY = newMin.Y; cState.OriginZ = newMin.Z;

            int index = 0; cState.UsedBlocks = 0;
            //cState.PasteAir = true;

            for (ushort y = newMin.Y; y <= newMax.Y; ++y)
                for (ushort z = newMin.Z; z <= newMax.Z; ++z)
                    for (ushort x = newMin.X; x <= newMax.X; ++x)
                    {
                        block = p.level.GetBlock(x, y, z);
                        if (!p.group.CanPlace[block]) { index++; continue; }

                        //if (block != Block.Air || cState.PasteAir) cState.UsedBlocks++;
                        cState.UsedBlocks++;
                        cState.Set(block, index);
                        index++;
                    }

            if (cState.UsedBlocks > p.group.DrawLimit)
            {
                p.Message("You tried to move {0} blocks. You cannot move more than {1} blocks.",
                          cState.UsedBlocks, p.group.DrawLimit);
                cState.Clear(); cState = null;
                p.ClearSelection();
                p.Extras.Remove("MOVESELECTION_MIN");
                p.Extras.Remove("MOVESELECTION_MAX");
                return false;
            }

            cState.CopySource = "level " + p.level.name;
            p.CurrentCopy = cState;

            p.Message("&SSelection created. Type &T/MoveSelection stop &Sto clear it, and &T/MoveSelection [direction] &Sto move it.");
            return true;
        }
    }
}
