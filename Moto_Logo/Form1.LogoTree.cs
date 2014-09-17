using System;
using System.Windows.Forms;

namespace Moto_Logo
{
    public partial class Form1
    {
        // ReSharper disable InconsistentNaming
        [Flags]
        private enum LOGO
        {
            LOGO_RAW = 0,
            LOGO_BOOT = 1,
            LOGO_BATTERY = 2,
            LOGO_UNLOCKED = 4,
            LOGO_LOWPOWER = 8,
            LOGO_UNPLUG = 0x10,
            LOGO_CHARGE = 0x20,
            KITKAT_DISABLED = 0x40000000
        };
        // ReSharper restore InconsistentNaming

        private void init_tree(LOGO logo)
        {
            init_tree((UInt32)logo);
        }

        private void init_tree(UInt32 logobincontents)
        {
            if (logobincontents == (int)LOGO.LOGO_RAW)
            {
                init_tree(LOGO.LOGO_UNLOCKED);
                rdoAndroid43.Enabled = false;
                rdoAndroid44.Enabled = false;
                rdoAndroidRAW.Checked = true;
                return;
            }
            var enableKitkat = ((logobincontents & (int)LOGO.KITKAT_DISABLED) == 0);
            rdoAndroid43.Enabled = true;
            rdoAndroid44.Enabled = enableKitkat;
            if (_autoselectlogobinversion && enableKitkat) rdoAndroid44.Checked = true;
            else if (_autoselectlogobinversion && rdoAndroid44.Checked) rdoAndroid43.Checked = true;
            init_tree((logobincontents & (UInt32)LOGO.LOGO_BOOT) == (UInt32)LOGO.LOGO_BOOT,
                (logobincontents & (UInt32)LOGO.LOGO_BATTERY) == (UInt32)LOGO.LOGO_BATTERY,
                (logobincontents & (UInt32)LOGO.LOGO_UNLOCKED) == (UInt32)LOGO.LOGO_UNLOCKED,
                (logobincontents & (UInt32)LOGO.LOGO_LOWPOWER) == (UInt32)LOGO.LOGO_LOWPOWER,
                (logobincontents & (UInt32)LOGO.LOGO_UNPLUG) == (UInt32)LOGO.LOGO_UNPLUG,
                (logobincontents & (UInt32)LOGO.LOGO_CHARGE) == (UInt32)LOGO.LOGO_CHARGE);
        }

        private bool Keeptreenode(TreeNode node, bool keep)
        {
            if (!keep && (cboMoto.SelectedIndex > 0)) node.Remove();
            return keep;
        }

        private void init_tree(bool logoboot, bool logobattery, 
            bool logounlocked, bool logolowpower, bool logounplug, bool logocharge)
        {
            var logoBoot = false;
            var logoBattery = false;
            var logoUnlocked = false;
            var logoLowpower = false;
            var logoUnplug = false;
            var logoCharge = false;
            for (var index = tvLogo.Nodes.Count - 1; index >= 0; index--)
            {
                var node = tvLogo.Nodes[index];
                switch (node.Text)
                {

                    case "logo_boot":
                        logoBoot = Keeptreenode(node, logoboot);
                        break;
                    case "logo_battery":
                        logoBattery = Keeptreenode(node, logobattery);
                        break;
                    case "logo_unlocked":
                        logoUnlocked = Keeptreenode(node, logounlocked);
                        break;
                    case "logo_lowpower":
                        logoLowpower = Keeptreenode(node, logolowpower);
                        break;
                    case "logo_unplug":
                        logoUnplug = Keeptreenode(node, logounplug);
                        break;
                    case "logo_charge":
                        logoCharge = Keeptreenode(node, logocharge);
                        break;
                }
            }
            if (!logoBoot && logoboot) tvLogo.Nodes.Add("logo_boot");
            if (!logoBattery && logobattery) tvLogo.Nodes.Add("logo_battery");
            if (!logoUnlocked && logounlocked) tvLogo.Nodes.Add("logo_unlocked");
            if (!logoLowpower && logolowpower) tvLogo.Nodes.Add("logo_lowpower");
            if (!logoUnplug && logounplug) tvLogo.Nodes.Add("logo_unplug");
            if (!logoCharge && logocharge) tvLogo.Nodes.Add("logo_charge");
            for (var index = tvLogo.Nodes.Count - 1; index >= 0; index--)
            {
                var node = tvLogo.Nodes[index];
                switch (node.Text)
                {

                    case "logo_boot":
                        node.ToolTipText = "Visible only with boot-loader locked phone.  It is suggested you remove" +
                                           " the picture that is in this entry, to save bytes in your logo.bin";
                        break;
                    case "logo_battery":
                        node.ToolTipText = "Visible when your phone has had its battery fully discharged, and you " +
                                           "plug your phone in to charge";
                        break;
                    case "logo_unlocked":
                        node.ToolTipText =
                            "Visible on boot-loader unlocked phones. What you put here is likely to look" +
                            " much better than the unlocked device warning. :)";
                        break;
                    case "logo_lowpower":
                        node.ToolTipText = "Visible when the phone has more than 3% power while fully powerd off. Not much more is known.\n" +
                            "This feature is only present on the Moto E";
                        break;
                    case "logo_unplug":
                        node.ToolTipText = "Visible when the phone is fully charged while plugged in and fully powered off.\n" +
                            "This feature is only present on the Moto E";
                        break;
                    case "logo_charge":
                        node.ToolTipText =
                            "Visible when your phone is plugged in while fully powered off, and the phone has more" +
                            " than 3% charge.  logo_battery is shown instead if it has 0-3% charge.\n";
                        break;
                }
            }


        }
    }
}
