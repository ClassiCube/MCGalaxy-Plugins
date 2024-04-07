using System;
using MCGalaxy;
using MCGalaxy.Tasks;

namespace PluginRainbowColors
{
    public sealed class RainbowPlugin : Plugin
    {
        public override string creator { get { return "Not UnknownShadow200"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
        public override string name { get { return "Rainbow"; } }

        SchedulerTask task;
        static ColorDesc desc = new ColorDesc(0, 0, 0);
        static long tick = 0;
        public override void Load(bool startup)
        {
            desc.Code = 'r';

            task = Server.MainScheduler.QueueRepeat(RainbowCallback, null,
                                                     TimeSpan.FromMilliseconds(100));
        }

        public override void Unload(bool shutdown)
        {
            Server.MainScheduler.Cancel(task);
        }

        static void RainbowCallback(SchedulerTask task)
        {
            tick += (long)task.Delay.TotalMilliseconds;
            SetDescColorHSV(ref desc, tick % 3600L / 10F, 1.0F, 1.0F);
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player p in players)
            {
                p.Session.SendSetTextColor(desc);
            }
        }

        //based on https://stackoverflow.com/a/1626232
        static void SetDescColorHSV(ref ColorDesc desc, double hue, double saturation, double value)
        {
            int hi = ((int)Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            byte v = (byte)Utils.Clamp((int)value, 0, 255);
            byte p = (byte)Utils.Clamp((int)(value * (1 - saturation)), 0, 255);
            byte q = (byte)Utils.Clamp((int)(value * (1 - f * saturation)), 0, 255);
            byte t = (byte)Utils.Clamp((int)(value * (1 - (1 - f) * saturation)), 0, 255);

            if (hi == 0)
            {
                desc.R = v;
                desc.G = t;
                desc.B = p;
            }
            else if (hi == 1)
            {
                desc.R = q;
                desc.G = v;
                desc.B = p;
            }
            else if (hi == 2)
            {
                desc.R = p;
                desc.G = v;
                desc.B = t;
            }
            else if (hi == 3)
            {
                desc.R = p;
                desc.G = q;
                desc.B = v;
            }
            else if (hi == 4)
            {
                desc.R = t;
                desc.G = p;
                desc.B = v;
            }
            else
            {
                desc.R = v;
                desc.G = p;
                desc.B = q;
            }
        }
    }
}