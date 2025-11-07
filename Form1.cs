using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;

namespace MIB_AlienScanner
{
    public partial class Form1 : Form
    {
        // === originals ===
        private int radarAngle = 0;
        private Random rand = new Random();
        private SoundPlayer beepSound = new SoundPlayer(@"C:\Windows\Media\notify.wav");
        private List<AlienBlip> blips = new List<AlienBlip>();

        // === polish/FX ===
        private List<int> sweepTrail = new List<int>();
        private bool booting = true;
        private bool flashNeuralyzer = false;
        private System.Windows.Forms.Timer BootTimer = new System.Windows.Forms.Timer();

        // === layout + UI ===
        private const int LEFT_COLUMN_WIDTH = 440;
        private const int LEFT_PADDING = 20;
        private const int TOP_PADDING = 20;
        private const int BOTTOM_PADDING = 20;

        private Panel HeaderPanel;
        private System.Windows.Forms.DataGridView LogGrid;

        // === name/country generators (kept) ===
        private readonly string[] Countries = {
            "USA","UK","Brazil","Egypt","Japan","Germany","Australia","Canada","Mexico",
            "India","South Africa","Russia","France","Italy","Spain","Turkey","Argentina",
            "Saudi Arabia","Nigeria","China"
        };
        private readonly string[] NamePrefixes = { "Zor", "Grax", "Vel", "Xan", "Thra", "Quo", "Neb", "Ul", "Vor", "Kry" };
        private readonly string[] NameSuffixes = { "g", "dor", "nix", "th", "ax", "ion", "goth", "mur", "rax", "zor" };

        // === world map ===
        private Bitmap mapBitmap = null;
        private readonly string MapPath = "worldmap.jpg";


        // private readonly string MapPath = @"Assets\worldmap_dark.png";

        // === incoming transmission banner ===
        private bool showTransmission = false;
        private int transmissionTicks = 0;   // frames remaining
        private const int TransmissionDuration = 50; // ~2.5s at 50ms
        private const string TransmissionText = "INCOMING TRANSMISSION";

        // === nearest-country data ===
        private class CountryCentroid
        {
            public string Name; public double Lat; public double Lon;
            public CountryCentroid(string name, double lat, double lon)
            { Name = name; Lat = lat; Lon = lon; }
        }

        private static readonly List<CountryCentroid> CountryCentroids = new List<CountryCentroid>
{
    new CountryCentroid("USA", 38.0, -97.0),
    new CountryCentroid("Canada", 56.0, -96.0),
    new CountryCentroid("Mexico", 23.0, -102.0),
    new CountryCentroid("Brazil", -10.0, -55.0),
    new CountryCentroid("Argentina", -34.0, -64.0),
    new CountryCentroid("Chile", -35.0, -71.0),
    new CountryCentroid("Colombia", 4.0, -73.0),
    new CountryCentroid("Peru", -9.0, -75.0),
    new CountryCentroid("UK", 54.0, -2.0),
    new CountryCentroid("Ireland", 53.0, -8.0),
    new CountryCentroid("France", 46.0, 2.0),
    new CountryCentroid("Spain", 40.0, -4.0),
    new CountryCentroid("Portugal", 39.5, -8.0),
    new CountryCentroid("Italy", 42.5, 12.5),
    new CountryCentroid("Germany", 51.0, 10.0),
    new CountryCentroid("Poland", 52.0, 19.0),
    new CountryCentroid("Ukraine", 49.0, 32.0),
    new CountryCentroid("Sweden", 62.0, 15.0),
    new CountryCentroid("Norway", 61.0, 8.0),
    new CountryCentroid("Finland", 64.0, 26.0),
    new CountryCentroid("Turkey", 39.0, 35.0),
    new CountryCentroid("Egypt", 26.0, 30.0),
    new CountryCentroid("Nigeria", 9.0, 8.0),
    new CountryCentroid("South Africa", -29.0, 24.0),
    new CountryCentroid("Saudi Arabia", 24.0, 45.0),
    new CountryCentroid("Iran", 32.0, 53.0),
    new CountryCentroid("Iraq", 33.0, 44.0),
    new CountryCentroid("India", 21.0, 78.0),
    new CountryCentroid("Pakistan", 30.0, 70.0),
    new CountryCentroid("China", 35.0, 103.0),
    new CountryCentroid("Mongolia", 46.0, 105.0),
    new CountryCentroid("Kazakhstan", 48.0, 68.0),
    new CountryCentroid("Japan", 36.0, 138.0),
    new CountryCentroid("South Korea", 36.0, 128.0),
    new CountryCentroid("Indonesia", -2.0, 118.0),
    new CountryCentroid("Philippines", 13.0, 122.0),
    new CountryCentroid("Australia", -25.0, 133.0),
    new CountryCentroid("Russia", 60.0, 90.0)
};


        public Form1()
        {
            InitializeComponent();

            // --- FULLSCREEN / KIOSK MODE ---
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = false; // set true if you want always on top
            this.BackColor = Color.Black;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.DoubleBuffered = true; // reduce flicker
            Cursor.Hide(); // hide mouse for kiosk look
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // timers
            RadarTimer.Interval = 50;
            AlertTimer.Interval = 3000;

            BootTimer.Interval = 120;
            BootTimer.Tick += BootTimer_Tick;

            // boot text
            LblStatus.ForeColor = Color.Lime;
            LblStatus.Text = "Initializing MIB Alien Detection Systems...";

            // load world map (safe)
            try { mapBitmap = new Bitmap(MapPath); } catch { mapBitmap = null; }

            // build left column
            BuildHeaderPanel();
            BuildLogGrid();

            // start boot; scanning begins after boot completes
            BootTimer.Start();
        }

        // ===== Boot sequence =====
        private int bootStep = 0;
        private readonly string[] bootLines =
        {
            "Loading biometric scanner...",
            "Activating radar sweep module...",
            "Linking deep-space telemetry...",
            "Decrypting alien frequency bands...",
            "Boot sequence complete. Welcome, Agent O."
        };

        private void BootTimer_Tick(object sender, EventArgs e)
        {
            LblStatus.Text = bootLines[bootStep];
            bootStep++;
            if (bootStep >= bootLines.Length)
            {
                BootTimer.Stop();
                booting = false;
                LblStatus.Text = "Scanning for extraterrestrial life...";
                RadarTimer.Start();
                AlertTimer.Start();
            }
        }

        // ===== Main animation timer =====
        private void RadarTimer_Tick(object sender, EventArgs e)
        {
            if (flashNeuralyzer)
            {
                this.BackColor = Color.White;
                this.Refresh();
                Thread.Sleep(100);
                flashNeuralyzer = false;
                this.BackColor = Color.Black;
                return;
            }

            radarAngle += 5;
            if (radarAngle >= 360) radarAngle = 0;

            // glow trail
            sweepTrail.Add(radarAngle);
            if (sweepTrail.Count > 20)
                sweepTrail.RemoveAt(0);

            // decay blips
            foreach (var b in blips.ToList())
            {
                b.Life -= 3;
                if (b.Life <= 0) blips.Remove(b);
            }

            // banner timer
            if (showTransmission)
            {
                transmissionTicks--;
                if (transmissionTicks <= 0) showTransmission = false;
            }

            RadarDisplay.Invalidate();
        }

        // ===== Drawing =====
        private void RadarDisplay_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 1) world map background (dimmed)
            if (mapBitmap != null)
            {
                var dest = RadarDisplay.ClientRectangle;
                g.DrawImage(mapBitmap, dest); // stretched
                using (Brush dimmer = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    g.FillRectangle(dimmer, dest);
            }
            else
            {
                g.Clear(Color.Black);
            }

            // 2) radar geometry
            var center = new Point(RadarDisplay.Width / 2 + LEFT_COLUMN_WIDTH / 4, RadarDisplay.Height / 2);
            int radius = Math.Min(RadarDisplay.Width - LEFT_COLUMN_WIDTH, RadarDisplay.Height) / 2 - 20;
            if (radius < 50) radius = 50;

            // 3) brighter grid rings
            using (Pen gridPen = new Pen(Color.FromArgb(80, 0, 255, 0), 1))
            {
                for (int i = 1; i <= 4; i++)
                {
                    g.DrawEllipse(gridPen,
                        center.X - radius * i / 4, center.Y - radius * i / 4,
                        radius * i / 2, radius * i / 2);
                }
            }

            // 4) pulsing center
            int pulse = (int)(Math.Abs(Math.Sin(Environment.TickCount / 300.0)) * 100);
            using (Brush pulseBrush = new SolidBrush(Color.FromArgb(pulse, 0, 255, 120)))
                g.FillEllipse(pulseBrush, center.X - 22, center.Y - 22, 44, 44);

            // 5) sweep trail (neon glow)
            for (int i = 0; i < sweepTrail.Count; i++)
            {
                int alpha = Math.Max(20, 140 - i * 6);
                using (Pen trailPen = new Pen(Color.FromArgb(alpha, 0, 255, 120), 2))
                {
                    g.DrawLine(trailPen, center,
                        new Point(center.X + (int)(radius * Math.Cos(sweepTrail[i] * Math.PI / 180)),
                                  center.Y + (int)(radius * Math.Sin(sweepTrail[i] * Math.PI / 180))));
                }
            }

            // 6) main sweep
            using (Pen sweepPen = new Pen(Color.FromArgb(0, 255, 120), 3))
            {
                g.DrawLine(sweepPen, center,
                    new Point(center.X + (int)(radius * Math.Cos(radarAngle * Math.PI / 180)),
                              center.Y + (int)(radius * Math.Sin(radarAngle * Math.PI / 180))));
            }

            // 7) blips (bright core + glow ring)
            foreach (var b in blips)
            {
                int alpha = Math.Max(120, Math.Min(255, b.Life * 3));
                using (Brush core = new SolidBrush(Color.FromArgb(alpha, 0, 255, 120)))
                    g.FillEllipse(core, b.X - 5, b.Y - 5, 10, 10);

                int ringAlpha = Math.Min(140, b.Life * 2);
                using (Pen ring = new Pen(Color.FromArgb(ringAlpha, 0, 255, 120), 2))
                    g.DrawEllipse(ring, b.X - 12, b.Y - 12, 24, 24);
            }

            // 8) incoming transmission banner
            if (showTransmission)
            {
                int flicker = 200 + (int)(55 * Math.Abs(Math.Sin(Environment.TickCount / 120.0)));
                using (Font f = new Font("Consolas", 16, FontStyle.Bold))
                using (Brush b = new SolidBrush(Color.FromArgb(flicker, 255, 0, 0)))
                {
                    var size = g.MeasureString(TransmissionText, f);
                    float x = RadarDisplay.Width - size.Width - 20;
                    float y = 15;
                    g.DrawString(TransmissionText, f, b, x, y);
                }
            }
        }

        // ===== Random detections =====
        private void AlertTimer_Tick(object sender, EventArgs e)
        {
            if (booting) return;

            int detectionChance = rand.Next(1, 8); // ~1/7 chance
            if (detectionChance == 3)
            {
                double x = rand.NextDouble() * RadarDisplay.Width;
                double y = rand.NextDouble() * RadarDisplay.Height;

                LblStatus.ForeColor = Color.Lime;
                LblStatus.Text = $"⚠️  Unidentified entity detected at ({x:F1}, {y:F1})";
                try { beepSound.Play(); } catch { }

                FlashForm();

                blips.Add(new AlienBlip { X = (int)x, Y = (int)y, Life = 100 });

                // NEW: find nearest country based on pixel location and log it
                string nearest = NearestCountryFromPixel((int)x, (int)y);
                LogDetection(x, y, nearest);

                showTransmission = true;
                transmissionTicks = TransmissionDuration;
            }
            else
            {
                LblStatus.ForeColor = Color.Lime;
                LblStatus.Text = "Scanning for extraterrestrial life...";
            }
        }

        // ===== Effects =====
        private void FlashForm()
        {
            this.BackColor = Color.Red;
            this.Refresh();
            Thread.Sleep(100);
            this.BackColor = Color.Black;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Application.Exit();

            if (e.KeyCode == Keys.Space)
            {
                LblStatus.ForeColor = Color.White;
                LblStatus.Text = "🌀 Neuralyzer activated. Please stand by...";
                flashNeuralyzer = true;
            }
        }

        // ===== Tracker helpers =====
        private string GenAlienName()
        {
            return $"{NamePrefixes[rand.Next(NamePrefixes.Length)]}" +
                   $"{NameSuffixes[rand.Next(NameSuffixes.Length)]}-" +
                   $"{rand.Next(10, 99)}";
        }

        // Wrapper kept for any existing calls
        private void LogDetection(double x, double y) => LogDetection(x, y, null);

        // NEW: accepts optional country override
        private void LogDetection(double x, double y, string countryOverride)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string name = GenAlienName();

            string country = !string.IsNullOrWhiteSpace(countryOverride)
                ? countryOverride
                : Countries[rand.Next(Countries.Length)];

            LogGrid.Rows.Add(time, name, country, $"({x:F1}, {y:F1})");

            if (LogGrid.Rows.Count > 200) LogGrid.Rows.RemoveAt(0);
            if (LogGrid.RowCount > 0)
                LogGrid.FirstDisplayedScrollingRowIndex = LogGrid.RowCount - 1;
        }

        // === nearest-country helpers ===

        // Convert RadarDisplay pixel to (lat, lon) for an equirectangular map stretched to the control
        private (double Lat, double Lon) PixelToLatLon(int px, int py)
        {
            double w = Math.Max(1, RadarDisplay.Width);
            double h = Math.Max(1, RadarDisplay.Height);

            double xn = px / w;  // 0..1 left->right
            double yn = py / h;  // 0..1 top->bottom

            double lon = xn * 360.0 - 180.0;   // -180..+180
            double lat = 90.0 - yn * 180.0;    // +90..-90

            return (lat, lon);
        }

        private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

        // Haversine great-circle distance
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private string NearestCountryFromPixel(int px, int py)
        {
            var (lat, lon) = PixelToLatLon(px, py);
            string best = "Unknown";
            double bestDist = double.MaxValue;

            foreach (var c in CountryCentroids)
            {
                double d = HaversineKm(lat, lon, c.Lat, c.Lon);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c.Name;
                }
            }
            return best;
        }

        private void BuildHeaderPanel()
        {
            HeaderPanel = new Panel
            {
                BackColor = Color.FromArgb(24, 24, 24),
                Size = new Size(LEFT_COLUMN_WIDTH - (LEFT_PADDING * 2), 120),
                Location = new Point(LEFT_PADDING, TOP_PADDING),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BorderStyle = BorderStyle.FixedSingle
            };

            var title = new Label
            {
                Text = "M.I.B – Alien Detection Console",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.Lime,
                AutoSize = true,
                Location = new Point(10, 10)
            };
            var line2 = new Label
            {
                Text = "Agency Node: DET-IT-ALPHA",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                AutoSize = true,
                Location = new Point(10, 42)
            };
            var line3 = new Label
            {
                Text = "Operator: Agent O",
                Font = new Font("Consolas", 10, FontStyle.Italic),
                ForeColor = Color.Gainsboro,
                AutoSize = true,
                Location = new Point(10, 64)
            };
            var line4 = new Label
            {
                Text = "Version 2.3 – HALLOWEEN OPS",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(10, 86)
            };

            HeaderPanel.Controls.Add(title);
            HeaderPanel.Controls.Add(line2);
            HeaderPanel.Controls.Add(line3);
            HeaderPanel.Controls.Add(line4);

            this.Controls.Add(HeaderPanel);
            HeaderPanel.BringToFront();
        }

        private void BuildLogGrid()
        {
            LogGrid = new System.Windows.Forms.DataGridView();
            LogGrid.Name = "LogGrid";
            LogGrid.ReadOnly = true;
            LogGrid.AllowUserToAddRows = false;
            LogGrid.RowHeadersVisible = false;
            LogGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            LogGrid.BackgroundColor = Color.Black;
            LogGrid.BorderStyle = BorderStyle.None;

            LogGrid.EnableHeadersVisualStyles = false;
            LogGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            LogGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Lime;
            LogGrid.DefaultCellStyle.BackColor = Color.Black;
            LogGrid.DefaultCellStyle.ForeColor = Color.Lime;
            LogGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(20, 60, 20);
            LogGrid.DefaultCellStyle.SelectionForeColor = Color.Lime;

            LogGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            LogGrid.GridColor = Color.FromArgb(0, 120, 0);
            LogGrid.ColumnHeadersHeight = 28;

            LogGrid.Columns.Add("Time", "Time");
            LogGrid.Columns.Add("Name", "Name");
            LogGrid.Columns.Add("Country", "Country");
            LogGrid.Columns.Add("Coords", "Coords");

            // Size: left column width; height ~260 (tweak to taste)
            LogGrid.Size = new Size(LEFT_COLUMN_WIDTH - (LEFT_PADDING * 2), 260);
            LogGrid.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

            this.Controls.Add(LogGrid);
            LogGrid.BringToFront();

            // Initial positioning at bottom-left (under header, but pinned to bottom)
            PositionLogGrid();

            // Keep it bottom-left on resize
            this.Resize -= Form1_Resize;
            this.Resize += Form1_Resize;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            PositionLogGrid();
        }

        private void PositionLogGrid()
        {
            if (LogGrid == null) return;

            // Keep a small gap from the bottom; align left with header
            int x = LEFT_PADDING;
            int y = this.ClientSize.Height - BOTTOM_PADDING - LogGrid.Height;
            if (y < HeaderPanel.Bottom + 10) y = HeaderPanel.Bottom + 10; // never overlap header

            LogGrid.Location = new Point(x, y);
        }

        public class AlienBlip
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Life { get; set; } = 100;
        }
    }
}
