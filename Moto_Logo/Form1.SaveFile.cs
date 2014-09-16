using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Moto_Logo.Properties;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;

namespace Moto_Logo
{
    public partial class Form1
    {

        private void SaveFileDialog(bool showDialog)
        {
            toolStripStatusLabel1.Text = "";
            Application.DoEvents();
            saveFileDialog1.Filter = Resources.ZipBins;
            if (showDialog && (saveFileDialog1.ShowDialog() != DialogResult.OK)) return;
            try
            {
                SaveFile();
            }
            catch (Exception ex)
            {
                ProgressBar.Visible = false;
                toolStripStatusLabel1.Text = @"Exception during processing: " + ex.GetBaseException();
            }
        }

        private byte[] encode_image(Bitmap img)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            ProgressBar.Visible = true;
            ProgressBar.Minimum = 0;
            ProgressBar.Value = 0;
            ProgressBar.Maximum = img.Height;
            Application.DoEvents();
            if (!rdoAndroid44.Checked)
            {
                for (var y = 0; y < 540; y++)
                {
                    for (var x = 0; x < 540; x++)
                    {
                        writer.Write(img.GetPixel(x, y).B);
                        writer.Write(img.GetPixel(x, y).G);
                        writer.Write(img.GetPixel(x, y).R);
                    }
                    ProgressBar.Value++;
                    Application.DoEvents();
                }
            }
            else
            {
                writer.Write(0x006E75526F746F4DL);
                writer.Write((byte)(img.Width >> 8));
                writer.Write((byte)(img.Width & 0xFF));
                writer.Write((byte)(img.Height >> 8));
                writer.Write((byte)(img.Height & 0xFF));

                for (var y = 0; y < img.Height; y++)
                {
                    var colors = new Color[img.Width];
                    for (var x = 0; x < img.Width; x++)
                        colors[x] = Color.FromArgb(255, img.GetPixel(x, y));
                    var compress = compress_row(colors);
                    writer.Write(compress);
                    ProgressBar.Value++;
                    Application.DoEvents();
                }
            }
            ProgressBar.Visible = false;
            Application.DoEvents();
            return stream.ToArray();
        }

        private static byte[] compress_row(IList<Color> colors)
        {
            var j = 0;
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            while (j < colors.Count)
            {
                var k = j;
                while ((k < colors.Count) && (colors[j] == colors[k]))
                {
                    k++;
                }
                if ((k - j) > 1)
                {
                    writer.Write((byte)(0x80 | ((k - j) >> 8)));
                    writer.Write((byte)((k - j) & 0xFF));
                    writer.Write(colors[j].B);
                    writer.Write(colors[j].G);
                    writer.Write(colors[j].R);
                    j = k;
                }
                else
                {
                    var l = k;
                    int m;
                    do
                    {
                        k = l - 1;
                        while ((l < colors.Count) && (colors[k] != colors[l]))
                        {
                            k++;
                            l++;
                        }
                        while ((l < colors.Count) && (colors[k] == colors[l]))
                        {
                            l++;
                        }
                        if (l == colors.Count)
                            break;
                        m = l;
                        while ((m < colors.Count) && (colors[l] == colors[m]))
                        {
                            m++;
                        }


                    } while (((l - k) < 3) && ((m - l) < 2));
                    if ((k - j) == 0)
                    {
                        writer.Write((byte)0);
                        writer.Write((byte)1);
                        writer.Write(colors[colors.Count - 1].B);
                        writer.Write(colors[colors.Count - 1].G);
                        writer.Write(colors[colors.Count - 1].R);
                        break;
                    }
                    if (k == (colors.Count - 1))
                        k++;

                    writer.Write((byte)((k - j) >> 8));
                    writer.Write((byte)((k - j) & 0xFF));
                    for (l = 0; l < (k - j); l++)
                    {
                        writer.Write(colors[j + l].B);
                        writer.Write(colors[j + l].G);
                        writer.Write(colors[j + l].R);
                    }
                    j = k;
                }
            }
            return stream.ToArray();
        }


        private void SaveFile()
        {
            var stream = new MemoryStream();
            var errorCount = 0;
            var blankCount = 0;
            var errorproceed = false;
            var fileext = Path.GetExtension(saveFileDialog1.FileName);

            using (var writer = new BinaryWriter(stream))
            {
                if (rdoAndroidRAW.Checked)
                {
                    try
                    {
                        _tvLogoAfterSelectProcessing = true;
                        SetRadioButtons(Convert.ToInt32(tvLogo.Nodes[0].Name));
                        var img = FixedSizeSave(_loadedbitmaps[Convert.ToInt32(tvLogo.Nodes[0].Name)]);
                        _tvLogoAfterSelectProcessing = false;
                        writer.Write(encode_image(img));
                    }
                    catch
                    {
                        if (tvLogo.Nodes[0].Name != "")
                        {
                            toolStripStatusLabel1.Text = @"Error loading image - Processing Aborted :(";
                            writer.Close();
                            return;
                        }
                        if (MessageBox.Show(@"Are you sure you wish to proceed with a blank image?",
                            @"Motorola Boot Logo Maker",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2) == DialogResult.No)
                        {
                            toolStripStatusLabel1.Text = @"Processing Aborted";
                            writer.Close();
                            return;
                        }
                        writer.Write(Resources._540x540);
                    }


                }
                else
                {
                    var logoBootEmpty = false;
                    var logoBootIndex = -1;
                    var logoUnlockedEmpty = false;
                    var logoUnlockedIndex = -1;

                    var hashes = new List<byte[]>();
                    var offsets = new List<int>();
                    var sizes = new List<int>();

                    writer.Write(0x6F676F4C6F746F4DL);
                    writer.Write((byte)0);
                    writer.Write(0x0D + (tvLogo.Nodes.Count * 0x20));
                    var android43 = rdoAndroid43.Checked;
                    for (var i = 0; i < tvLogo.Nodes.Count; i++)
                    {
                        writer.BaseStream.Position = 0x0D + (i * 0x20);
                        var name = Encoding.ASCII.GetBytes(tvLogo.Nodes[i].Text);
                        writer.Write(name);
                        writer.Write(new byte[0x20 - name.Length]);
                        switch (tvLogo.Nodes[i].Text)
                        {
                            case "logo_boot":
                                logoBootIndex = i;
                                logoBootEmpty = tvLogo.Nodes[i].Name == "";
                                break;
                            case "logo_unlocked":
                                logoUnlockedIndex = i;
                                logoUnlockedEmpty = tvLogo.Nodes[i].Name == "";
                                break;
                        }
                    }
                    var sectorfillstr = Encoding.ASCII.GetBytes("*---==|This Boot logo was created with \"" +
                                    Application.ProductName + " " +
                                    Application.ProductVersion + "\" written by CaitSith2|==---*");
                    writer.Write(-2);
                    var cboMotoItem = (string)cboMoto.SelectedItem;
                    writer.Write(Encoding.UTF8.GetBytes(cboMotoItem).Length);
                    writer.Write(Encoding.UTF8.GetBytes(cboMotoItem));
                    writer.Write(sectorfillstr.Length);
                    writer.Write(sectorfillstr);
                    writer.Write(Encoding.UTF8.GetBytes(txtComments.Text).Length);
                    writer.Write(Encoding.UTF8.GetBytes(txtComments.Text));
                    writer.Write((UInt16)udResolutionX.Value);
                    writer.Write((UInt16)udResolutionY.Value);


                    var bothLogoEmpty = ((logoBootIndex == -1) || logoBootEmpty) &&
                                         ((logoUnlockedIndex == -1) || logoUnlockedEmpty);
                    for (var i = 0; i < tvLogo.Nodes.Count; i++)
                    {
                        toolStripStatusLabel1.Text = @"Processing " + tvLogo.Nodes[i].Text;

                        while ((writer.BaseStream.Position % 0x200) != 0)
                            writer.Write((byte)0xFF);
                        byte[] result;
                        try
                        {
                            _tvLogoAfterSelectProcessing = true;
                            SetRadioButtons(Convert.ToInt32(tvLogo.Nodes[i].Name));
                            var img = FixedSizeSave(_loadedbitmaps[Convert.ToInt32(tvLogo.Nodes[i].Name)]);
                            result = encode_image(img);
                            _tvLogoAfterSelectProcessing = false;
                            if (!errorproceed && (errorCount > 0))
                            {
                                if (MessageBox.Show(@"At least one image failed to load, " +
                                    @"are you sure you wish to proceed?",
                                    @"Motorola Boot Logo Maker",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2) == DialogResult.No)
                                {
                                    toolStripStatusLabel1.Text = @"Processing Aborted";
                                    writer.Close();
                                    return;
                                }
                                errorproceed = true;
                            }
                        }
                        catch
                        {
                            if (tvLogo.Nodes[i].Name != "")
                                errorCount++;
                            else
                                blankCount++;
                            if ((blankCount == tvLogo.Nodes.Count) &&
                               (MessageBox.Show(@"No images were loaded, are you sure you wish to" +
                               @" proceed with blank images?",
                               @"Motorola Boot Logo Maker",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question,
                               MessageBoxDefaultButton.Button2) == DialogResult.No))
                            {
                                toolStripStatusLabel1.Text = @"Processing Aborted";
                                writer.Close();
                                return;
                            }
                            if (((errorCount + blankCount) == tvLogo.Nodes.Count) && (errorCount > 0))
                            {
                                toolStripStatusLabel1.Text = @"Every single image selected failed to load" +
                                                             @" - Processing Aborted :(";
                                writer.Close();
                                return;
                            }
                            _tvLogoAfterSelectProcessing = true;
                            SetRadioButtons(ImageOption.ImageOptionCenter, ImageLayout.ImageLayoutPortrait);
                            _tvLogoAfterSelectProcessing = false;

                            switch (tvLogo.Nodes[i].Text)
                            {
                                case "logo_lowpower":
                                    result = encode_image(FixedSizeSave(Resources.logo_lowpower));
                                    break;
                                case "logo_battery":
                                    result = encode_image(FixedSizeSave(Resources.logo_battery));
                                    break;
                                case "logo_unplug":
                                    result = encode_image(FixedSizeSave(Resources.logo_unplug));
                                    break;
                                case "logo_charge":
                                    result = encode_image(FixedSizeSave(Resources.logo_charge));
                                    break;
                                case "logo_unlocked":
                                case "logo_boot":
                                    if (!bothLogoEmpty)
                                        continue;
                                    result = encode_image(FixedSizeSave(Resources.logo_boot));
                                    break;
                                default:
                                    result = android43
                                        ? Resources._540x540
                                        : Resources.motorun;
                                    break;
                            }

                        }

                        var tempoffset = (int)writer.BaseStream.Position;
                        var tempsize = result.Length;
                        var hash = SHA256.Create().ComputeHash(result);
                        var hashmatch = false;
                        for (var j = 0; j < hashes.Count; j++)
                        {
                            if (!hashes[j].SequenceEqual(hash)) continue;
                            hashmatch = true;
                            tempoffset = offsets[j];
                            tempsize = sizes[j];
                            break;
                        }

                        if ((logoBootIndex == i) && (logoUnlockedIndex > -1) && logoUnlockedEmpty)
                        {
                            writer.BaseStream.Position = 0x0D + (logoUnlockedIndex * 0x20) + 0x18;
                            writer.Write(tempoffset);
                            writer.Write(tempsize);
                            writer.BaseStream.Position = tempoffset;
                        }
                        if ((logoUnlockedIndex == i) && (logoBootIndex > -1) && logoBootEmpty)
                        {
                            writer.BaseStream.Position = 0x0D + (logoBootIndex * 0x20) + 0x18;
                            writer.Write(tempoffset);
                            writer.Write(tempsize);
                            writer.BaseStream.Position = tempoffset;
                        }


                        if (!hashmatch)
                        {
                            hashes.Add(hash);
                            offsets.Add(tempoffset);
                            sizes.Add(result.Length);
                            writer.Write(result);
                        }

                        writer.BaseStream.Position = 0x0D + 0x18 + (i * 0x20);
                        writer.Write(tempoffset);
                        writer.Write(tempsize);
                        writer.BaseStream.Position = writer.BaseStream.Length;
                        if (writer.BaseStream.Length <= _maxFileSize) continue;
                        toolStripStatusLabel1.Text =
                            @"Error: Images/options selected will not fit in logo.bin, Failed at " +
                            tvLogo.Nodes[i].Text + @" Produced file is " +
                            (writer.BaseStream.Length - _maxFileSize) + @" Bytes Too Large";
                        return;
                    }
                }
            }

            if (fileext == ".zip")
            {
                var zipfilename = saveFileDialog1.FileName;

                using (var zip = new ZipFile())
                {
                    zip.AddEntry("logo.bin", stream.ToArray());
                    zip.AddDirectoryByName("META-INF");
                    zip.AddDirectoryByName("META-INF\\com");
                    zip.AddDirectoryByName("META-INF\\com\\google");
                    zip.AddDirectoryByName("META-INF\\com\\google\\android");

                    zip.AddEntry("META-INF\\com\\google\\android\\updater-script", Resources.updater_script);
                    zip.AddEntry("META-INF\\com\\google\\android\\update-binary", Resources.update_binary);
                    zip.Save(zipfilename);
                }
            }
            else
                File.WriteAllBytes(saveFileDialog1.FileName, stream.ToArray());

            toolStripStatusLabel1.Text = @"Processing Complete :)";
            _fileSaved = true;
        }
    }
}
