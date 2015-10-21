// ReSharper disable EmptyGeneralCatchClause
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Moto_Logo.Properties;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace Moto_Logo
{
    public partial class Form1 : Form
    {

        enum ImageOption
        {
            ImageOptionCenter,
            ImageOptionStretchProportionately,
            ImageOptionFill
        };

        enum ImageLayout
        {
            ImageLayoutPortrait,
            ImageLayoutLandscape
        };

        private bool _fileSaved;
        private bool _autoselectlogobinversion = true;
        private int _maxFileSize = 4*1024*1024; //4MiB
        
        private readonly List<String> _loadedbitmapnames = new List<string>(); 
        private readonly List<Bitmap> _loadedbitmaps = new List<Bitmap>();
        private readonly List<ImageOption> _loadedbitmapimageoptions = new List<ImageOption>();
        private readonly List<ImageLayout> _loadedbitmapimagelayout = new List<ImageLayout>();

        private readonly List<int> _deviceResolutionX = new List<int>();
        private readonly List<int> _deviceResolutionY = new List<int>();
        private readonly List<int> _deviceLogoBinSize = new List<int>();
        private readonly List<UInt32> _deviceLogoBinContents = new List<UInt32>();

        private Image FixedSizePreview(Image imgPhoto)
        {
            return FixedSize(!rdoAndroid44.Checked ? FixedSize(imgPhoto, 540, 540) : imgPhoto, 
                (int) udResolutionX.Value, (int) udResolutionY.Value,
                !rdoAndroid44.Checked);
        }

        private Bitmap FixedSizeSave(Image imgPhoto)
        {
            var xmax = rdoAndroid44.Checked ? (int)udResolutionX.Value : 540;
            var ymax = rdoAndroid44.Checked ? (int)udResolutionY.Value : 540;
            return (rdoImageCenter.Checked && (imgPhoto.Width <= xmax) 
                    && (imgPhoto.Height <= ymax) && rdoAndroid44.Checked)
                        ? (Bitmap)imgPhoto
                        : FixedSize(imgPhoto,xmax,ymax);
        }

        private Bitmap FixedSize(Image imgPhoto, int imgWidth, int imgHeight, bool forceCenter = false)
        {
            var landscape = (Image)imgPhoto.Clone();
            landscape.RotateFlip(RotateFlipType.Rotate90FlipNone);
            var img = (rdoLayoutLandscape.Checked ? landscape : imgPhoto);

            var sourceWidth = img.Width;
            var sourceHeight = img.Height;
            const int sourceX = 0;
            const int sourceY = 0;
            var destX = 0;
            var destY = 0;

            float nPercent = 0;


// ReSharper disable RedundantCast
            var nPercentW = ((float)imgWidth / (float)sourceWidth);
            var nPercentH = ((float)imgHeight / (float)sourceHeight);
// ReSharper restore RedundantCast

            if (((sourceWidth <= imgWidth) && (sourceHeight <= imgHeight)) && (rdoImageCenter.Checked || forceCenter))
            {
                nPercent = 1.0f;
                destX = (imgWidth - sourceWidth)/2;
                destY = (imgHeight - sourceHeight)/2;
            }
            else if ((nPercentH < nPercentW) && (!rdoImageFill.Checked || forceCenter))
            {
                nPercent = nPercentH;
                destX = Convert.ToInt16((imgWidth -
                              (sourceWidth * nPercent)) / 2);
            }
            else if (!rdoImageFill.Checked || forceCenter)
            {
                nPercent = nPercentW;
                destY = Convert.ToInt16((imgHeight -
                              (sourceHeight * nPercent)) / 2);
            }

            var destWidth = (int)(sourceWidth * ((rdoImageFill.Checked && !forceCenter) ? nPercentW : nPercent));
            var destHeight = (int)(sourceHeight * ((rdoImageFill.Checked && !forceCenter) ? nPercentH : nPercent));

            var bmPhoto = new Bitmap(imgWidth, imgHeight,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(img.HorizontalResolution,
                             img.VerticalResolution);

            var grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(((Bitmap) img).GetPixel(0, 0));
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            

            grPhoto.DrawImage(img,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }


        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void udResolutionX_ValueChanged(object sender, EventArgs e)
        {
            tvLogo_AfterSelect(sender, null);
        }

        private void AddToBitmapList(Bitmap img, String filename, String logoname)
        {
            
            var nodeindex = -1;
            for (var i = 0; i < tvLogo.Nodes.Count; i++)
            {
                if (tvLogo.Nodes[i].Text != logoname) continue;
                nodeindex = i;
                break;
            }
            if (nodeindex == -1)
            {
                tvLogo.Nodes.Add(logoname);
                nodeindex = tvLogo.Nodes.Count - 1;
            }
            try
            {
                

                if (_loadedbitmaps.IndexOf(img) != -1) return;
                _loadedbitmaps.Add(img);
                tvLogo.Nodes[nodeindex].Name = _loadedbitmaps.IndexOf(img).ToString();
                _loadedbitmapnames.Add(filename);
                _loadedbitmapimageoptions.Add(rdoImageCenter.Checked
                    ? ImageOption.ImageOptionCenter
                    : rdoImageStretchAspect.Checked
                        ? ImageOption.ImageOptionStretchProportionately
                        : ImageOption.ImageOptionFill);
                _loadedbitmapimagelayout.Add(rdoLayoutLandscape.Checked
                    ? ImageLayout.ImageLayoutLandscape
                    : ImageLayout.ImageLayoutPortrait);
            }
            catch
            {
                tvLogo.Nodes[nodeindex].Name = "";
            }
        }

        private void ClearBitmapList()
        {
            _loadedbitmaps.Clear();
            _loadedbitmapnames.Clear();
            _loadedbitmapimageoptions.Clear();
            _loadedbitmapimagelayout.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((cboMoto.SelectedIndex > 0) && button1.Text == Resources.Append) return;
            if (txtLogoInternalFile.Text == "") return;

            if (button1.Text == Resources.Append)
                switch (txtLogoInternalFile.Text)
                {
                    case "logo_boot":
                        init_tree(LOGO.LOGO_BOOT);
                        break;
                    case "logo_battery":
                        init_tree(LOGO.LOGO_BATTERY);
                        break;
                    case "logo_unlocked":
                        init_tree(LOGO.LOGO_UNLOCKED);
                        break;
                    case "logo_lowpower":
                        init_tree(LOGO.LOGO_LOWPOWER);
                        break;
                    case "logo_unplug":
                        init_tree(LOGO.LOGO_UNPLUG);
                        break;
                    case "logo_charge":
                        init_tree(LOGO.LOGO_CHARGE);
                        break;
                }

            openFileDialog1.Filter = Resources.SelectImageFile;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var img = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialog1.FileName)));
                AddToBitmapList(img, Path.GetFileName(openFileDialog1.FileName), txtLogoInternalFile.Text);
                toolStripStatusLabel1.Text = openFileDialog1.FileName;
            }
            else
            {
                var nodeFound = false;
                foreach (var node in tvLogo.Nodes.Cast<TreeNode>().Where(node => node.Text == txtLogoInternalFile.Text))
                {
                    node.Name = "";
                    nodeFound = true;
                }
                if (!nodeFound) tvLogo.Nodes.Add(txtLogoInternalFile.Text);
            }
            button1.Text = Resources.Replace;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (tvLogo.SelectedNode == null) return;
            if((cboMoto.SelectedIndex == 0))
                tvLogo.SelectedNode.Remove();
            else
            {
                tvLogo.SelectedNode.Name = "";
                if (tvLogo.Nodes.Count == 0)
                    ClearBitmapList();
            }
        }

        private void SetRadioButtons(ImageOption imageOption, ImageLayout imageLayout)
        {
            switch (imageOption)
            {
                case ImageOption.ImageOptionCenter:
                    rdoImageCenter.Checked = true;
                    break;
                case ImageOption.ImageOptionFill:
                    rdoImageFill.Checked = true;
                    break;
                case ImageOption.ImageOptionStretchProportionately:
                    rdoImageStretchAspect.Checked = true;
                    break;
            }
            switch (imageLayout)
            {
                case ImageLayout.ImageLayoutPortrait:
                    rdoLayoutPortrait.Checked = true;
                    break;
                case ImageLayout.ImageLayoutLandscape:
                    rdoLayoutLandscape.Checked = true;
                    break;
            }
        }

        private void SetRadioButtons(int index)
        {
            SetRadioButtons(_loadedbitmapimageoptions[index], _loadedbitmapimagelayout[index]);
        }

        bool _tvLogoAfterSelectProcessing;
        private void tvLogo_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (tvLogo.SelectedNode == null) return;
            if (_tvLogoAfterSelectProcessing) return;
            txtLogoInternalFile.Text = tvLogo.SelectedNode.Text;
            _tvLogoAfterSelectProcessing = true;
            try
            {
                var index = Convert.ToInt32(tvLogo.SelectedNode.Name);
                var bitmap = _loadedbitmaps[index];
                if (bitmap == null) return;
                SetRadioButtons(index);
                pictureBox1.Image = FixedSizePreview(bitmap);
                toolStripStatusLabel1.Text = _loadedbitmapnames[index]
                    + @": " + bitmap.Width + @"x" + bitmap.Height;
                Application.DoEvents();
            }
            catch
            {
                SetRadioButtons(ImageOption.ImageOptionCenter,ImageLayout.ImageLayoutPortrait);
                switch (tvLogo.SelectedNode.Text)
                {
                    default:
                        pictureBox1.Image = new Bitmap(1, 1);
                        break;
                    case "logo_lowpower":
                        pictureBox1.Image = FixedSizePreview(Resources.logo_lowpower);
                        break;
                    case "logo_battery":
                        pictureBox1.Image = FixedSizePreview(Resources.logo_battery);
                        break;
                    case "logo_unplug":
                        pictureBox1.Image = FixedSizePreview(Resources.logo_unplug);
                        break;
                    case "logo_charge":
                        pictureBox1.Image = FixedSizePreview(Resources.logo_charge);
                        break;
                    case "logo_boot":
                    case "logo_unlocked":
                        pictureBox1.Image = FixedSizePreview(Resources.logo_boot);
                        break;
                }
                toolStripStatusLabel1.Text = "";
                Application.DoEvents();
            }
            _tvLogoAfterSelectProcessing = false;
        }

        private void tvLogo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (tvLogo.SelectedNode == null) return;
            openFileDialog1.Filter = Resources.SelectImageFile;
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            var img = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialog1.FileName)));
            AddToBitmapList(img, Path.GetFileName(openFileDialog1.FileName), tvLogo.SelectedNode.Text);
            toolStripStatusLabel1.Text = openFileDialog1.FileName;
            tvLogo_AfterSelect(sender, null);
        }

        private void txtLogoInternalFile_TextChanged(object sender, EventArgs e)
        {
            button1.Text = tvLogo.Nodes.Cast<TreeNode>().Any(node => node.Text == txtLogoInternalFile.Text) 
                ? Resources.Replace 
                : Resources.Append;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var temp = Settings.Default.MotoDevice;
            openFileDialog1.Filter = @"Logo Files|*.zip;*.bin|Bin Files|*.bin|Flashable Zip files|*.zip|All Files|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            OpenFile(openFileDialog1.FileName);
            Settings.Default.MotoDevice = temp;
            Settings.Default.Save();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog(!_fileSaved);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog(true);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _loadedbitmaps.Clear();
            _loadedbitmapnames.Clear();
            _fileSaved = false;
            rdoAndroid44.Checked = true;
            cboMoto.SelectedIndex = Settings.Default.MotoDevice;
            tvLogo.Nodes.Clear();
            txtComments.Text = "";
            cboMoto_SelectedIndexChanged(sender,e);
            toolStripStatusLabel1.Text = "";
            Application.DoEvents();
            pictureBox1.Image = new Bitmap(1, 1);
            rdoImageCenter.Checked = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Init_cboMoto("Custom",720,1280,4194304,0x3FFFFFFF);
            Init_cboMoto("Moto X Style (3rd Gen)", 1440, 2560, 8388608, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_LOWPOWER | LOGO.LOGO_CHARGE));
            Init_cboMoto("Moto X Play (3rd Gen)", 1080, 1920, 6291456, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_LOWPOWER | LOGO.LOGO_CHARGE));
            Init_cboMoto("Moto X (2nd Gen)", 1080, 1920, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_LOWPOWER | LOGO.LOGO_CHARGE));
            Init_cboMoto("Moto X (1st Gen)", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED));
            Init_cboMoto("Moto E (1st/2nd Gen)", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_LOWPOWER | LOGO.LOGO_UNPLUG));
            Init_cboMoto("Moto G (1st/2nd/3rd Gen)", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_CHARGE));
            Init_cboMoto("Droid Ultra", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED));
            Init_cboMoto("Droid RAZR HD", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("RAZR i", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Droid RAZR M", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Photon Q 4G LTE", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Atrix HD", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Droid 4", 540,960,1048576,(int)LOGO.LOGO_RAW);
            Init_cboMoto("Atrix 2", 540, 960, 1048576, (int)LOGO.LOGO_RAW);
            Init_cboMoto("Droid RAZR", 540, 960, 1048576, (int)LOGO.LOGO_RAW);
            Init_cboMoto("Photon 4G", 540, 960, 1048576, (int)LOGO.LOGO_RAW);

            newToolStripMenuItem_Click(sender, e);
        }

        private void rdoAndroid43_CheckedChanged(object sender, EventArgs e)
        {
            if (_tvLogoAfterSelectProcessing) return;
            if (tvLogo.SelectedNode == null) return;
            if (string.IsNullOrEmpty(tvLogo.SelectedNode.Name)) return;
            var index = Convert.ToInt32(tvLogo.SelectedNode.Name);
            _loadedbitmapimageoptions[index] = rdoImageCenter.Checked
                ? ImageOption.ImageOptionCenter
                : rdoImageStretchAspect.Checked
                    ? ImageOption.ImageOptionStretchProportionately
                    : ImageOption.ImageOptionFill;
            _loadedbitmapimagelayout[index] = rdoLayoutLandscape.Checked
                ? ImageLayout.ImageLayoutLandscape
                : ImageLayout.ImageLayoutPortrait;


            tvLogo_AfterSelect(sender, null);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Bitmap img;
            if (tvLogo.SelectedNode == null) return;
            try
            {
                img = File.Exists(tvLogo.SelectedNode.Name)
                    ? new Bitmap(new MemoryStream(File.ReadAllBytes(tvLogo.SelectedNode.Name)))
                    : _loadedbitmaps[Convert.ToInt32(tvLogo.SelectedNode.Name)];
            }
            catch (Exception)
            {
                switch (tvLogo.SelectedNode.Text)
                {
                    default:
                        return;
                    case "logo_lowpower":
                        img = Resources.logo_lowpower;
                        break;
                    case "logo_battery":
                        img = Resources.logo_battery;
                        break;
                    case "logo_unplug":
                        img = Resources.logo_unplug;
                        break;
                    case "logo_charge":
                        img = Resources.logo_charge;
                        break;
                }
            }
            saveFileDialog2.Filter = @"Png file|*.png|Jpeg file|*.jpg|Bitmap File|*.bmp|Gif file|*.gif|All Files|*.*";
            if (saveFileDialog2.ShowDialog() != DialogResult.OK) return;
            try
            {
                

                switch (Path.GetExtension(saveFileDialog2.FileName))
                {
                    case ".gif":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Gif);
                        break;
                    case ".jpg":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Jpeg);
                        break;
                    case ".bmp":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Bmp);
                        break;
                    default:
                        img.Save(saveFileDialog2.FileName, ImageFormat.Png);
                        break;

                }
                toolStripStatusLabel1.Text = @"Image saved as " + Path.GetFileName(saveFileDialog2.FileName) +
                                             @" Successfully :)";
                Application.DoEvents();
            }
            catch (Exception)
            {
                toolStripStatusLabel1.Text = @"Unable to Extract Image from bootlogo :(";
                Application.DoEvents();
            }
        }

        private void rdoAndroidRAW_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoAndroidRAW.Checked)
            {
                if (tvLogo.Nodes.Count <= 1) return;
                for(var i = tvLogo.Nodes.Count-1;i >= 0; i--)
                {
                    if (tvLogo.Nodes[i].Text == @"logo_unlocked") continue;
                    if(tvLogo.Nodes.Count > 1)
                        tvLogo.Nodes[i].Remove();
                }
            }
            else
            {
                _autoselectlogobinversion = false;
                cboMoto_SelectedIndexChanged(sender, e);
                _autoselectlogobinversion = true;
            }
            tvLogo_AfterSelect(sender, null);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if((files.Count() == 1) && ((Path.GetExtension(files[0]) == ".bin") || 
                                        (Path.GetExtension(files[0]) == ".zip")))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if ((files.Count() == 1) && ((Path.GetExtension(files[0]) == ".bin") ||
                                         (Path.GetExtension(files[0]) == ".zip")))
            {
                OpenFile(files[0]);
            }
        }

        private void udResolutionY_ValueChanged(object sender, EventArgs e)
        {
            tvLogo_AfterSelect(sender, null);
        }

        private void Init_cboMoto(string device, int resolutionX, int resolutionY, int logobinsize, UInt32 logoContents)
        {
            cboMoto.Items.Add(device);
            _deviceResolutionX.Add(resolutionX);
            _deviceResolutionY.Add(resolutionY);
            _deviceLogoBinSize.Add(logobinsize);
            _deviceLogoBinContents.Add(logoContents);
        }

        private void cboMoto_SelectedIndexChanged(object sender, EventArgs e)
        {
            var idx = cboMoto.SelectedIndex;
            Settings.Default.MotoDevice = idx;
            Settings.Default.Save();
            udResolutionX.Enabled = (idx == 0);
            udResolutionY.Enabled = (idx == 0);
            udResolutionX.Value = _deviceResolutionX[idx];
            udResolutionY.Value = _deviceResolutionY[idx];
            _maxFileSize = _deviceLogoBinSize[idx];
            init_tree(_deviceLogoBinContents[idx]);
            toolStripStatusLabel1.Text = @"Max Logo.bin size = " + (_maxFileSize / 1024 / 1024) + @"MiB";
        }
    }
}
