using NetPulseApp.Managers;
using System.Text;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>A results row: cell texts plus an optional accent applied to
    /// one column (e.g. the STATUS cell colored red for a timeout).</summary>
    public class TableRow
    {
        public string[] Cells { get; }
        public int AccentColumn { get; }
        public Color? Accent { get; }

        public TableRow(string[] cells, int accentColumn = -1, Color? accent = null)
        {
            Cells = cells;
            AccentColumn = accentColumn;
            Accent = accent;
        }
    }

    /// <summary>
    /// Shared themed results table for the tool pages (Ping / Traceroute /
    /// DNS / Port Checker all use the same CHECK/STATUS grid pattern).
    /// Owner-drawn ListView in virtual mode — theme-aware header/rows, one
    /// optionally-accented cell per row, TSV export, auto-scroll to newest.
    /// The hosting page calls ApplyTheme() on ThemeManager.ThemeChanged.
    /// </summary>
    public class ResultsTableControl : ListView
    {
        private const int MaxRows = 5000; // bounded log for long continuous runs
        private const int RowHeight = 26;

        private readonly List<TableRow> _rows = new();
        private (string Caption, int Width)[] _columnDefs = Array.Empty<(string, int)>();
        private string[] _columnCaptions = Array.Empty<string>();
        private bool _headerBold = true;

        public ResultsTableControl()
        {
            View = View.Details;
            FullRowSelect = true;
            HeaderStyle = ColumnHeaderStyle.Nonclickable;
            HideSelection = true;
            MultiSelect = false;
            GridLines = false;
            OwnerDraw = true;
            VirtualMode = true;
            BorderStyle = BorderStyle.None;
            Font = new Font("Segoe UI", 9f);

            // Dummy image list sizes the rows (ListView has no RowHeight).
            SmallImageList = new ImageList { ImageSize = new Size(1, RowHeight) };

            RetrieveVirtualItem += (s, e) => e.Item = BuildItem(e.ItemIndex);
            DrawColumnHeader += OnDrawColumnHeader;
            DrawItem += OnDrawItem;
            DrawSubItem += OnDrawSubItem;
            Resize += (s, e) => StretchLastColumn();

            // Row height and column widths must match the real monitor DPI —
            // neither is scaled by AutoScaleMode.Dpi and the control is built
            // before it's parented (DeviceDpi still 96), so apply them once
            // the handle exists (DpiAwareService.Scale is deliberately inert,
            // see its ScaleFactor comment).
            HandleCreated += (s, e) =>
            {
                float factor = DeviceDpi / 96f;
                if (factor > 1.01f)
                    SmallImageList = new ImageList
                    {
                        ImageSize = new Size(1, (int)Math.Round(RowHeight * factor))
                    };
                ApplyColumnWidths();
            };

            ApplyTheme();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Defines the columns. A width of 0 stretches to fill.</summary>
        public void SetColumns(params (string Caption, int Width)[] columns)
        {
            _columnDefs = columns;
            _columnCaptions = columns.Select(c => c.Caption).ToArray();
            ApplyColumnWidths();
        }

        /// <summary>Bolds the header captions (default true).</summary>
        public void SetHeaderBold(bool bold) => _headerBold = bold;

        public void AddRow(TableRow row)
        {
            _rows.Add(row);
            if (_rows.Count > MaxRows)
                _rows.RemoveAt(0);

            VirtualListSize = _rows.Count;
            EnsureVisible(_rows.Count - 1);
        }

        public void ClearRows()
        {
            _rows.Clear();
            VirtualListSize = 0;
        }

        public int RowCount => _rows.Count;

        /// <summary>Header + all rows as tab-separated text (clipboard/export).</summary>
        public string GetAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", _columnCaptions));
            foreach (var row in _rows)
                sb.AppendLine(string.Join("\t", row.Cells));
            return sb.ToString();
        }

        public void ApplyTheme()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            BackColor = theme.CardBackground;
            ForeColor = theme.TextColor;
            Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ITEM SOURCE (virtual mode)
        // ─────────────────────────────────────────────────────────────────────

        private ListViewItem BuildItem(int index)
        {
            var row = _rows[index];
            var item = new ListViewItem(row.Cells[0]);
            for (int i = 1; i < row.Cells.Length; i++)
                item.SubItems.Add(row.Cells[i]);
            return item;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PAINTING
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;

            using var bg = new SolidBrush(theme.SecondaryBackground);
                e.Graphics.FillRectangle(bg, e.Bounds);

            TextRenderer.DrawText(e.Graphics,
                _columnCaptions[e.ColumnIndex].ToUpperInvariant(),
                new Font("Segoe UI", 8f, _headerBold ? FontStyle.Bold : FontStyle.Regular),
                Pad(e.Bounds),
                theme.SecondaryTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using var border = new Pen(theme.BorderColor);
                e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            using var bg = new SolidBrush(e.Item.Selected ? theme.SelectedColor : theme.CardBackground);
                e.Graphics.FillRectangle(bg, e.Bounds);

            // Row separator
            using var line = new Pen(theme.BorderColor);
                e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var row = _rows[e.ItemIndex];

            Color color = theme.TextColor;
            FontStyle style = FontStyle.Regular;
            if (e.ColumnIndex == row.AccentColumn && row.Accent.HasValue)
            {
                color = row.Accent.Value;
                style = FontStyle.Bold;
            }

            string text = e.ColumnIndex < row.Cells.Length ? row.Cells[e.ColumnIndex] : "";
            TextRenderer.DrawText(e.Graphics, text,
                new Font("Segoe UI", 9f, style),
                Pad(e.Bounds),
                color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        /// <summary>Scale a base-pixel value by this control's actual DPI.</summary>
        private int Px(int value) => (int)Math.Round(value * DeviceDpi / 96f);

        private Rectangle Pad(Rectangle bounds) =>
            Rectangle.FromLTRB(bounds.Left + Px(8), bounds.Top, bounds.Right - Px(4), bounds.Bottom);

        /// <summary>(Re)builds the column headers at the control's actual DPI.</summary>
        private void ApplyColumnWidths()
        {
            Columns.Clear();
            foreach (var (caption, width) in _columnDefs)
                Columns.Add(caption, Px(width <= 0 ? 120 : width));
            StretchLastColumn();
        }

        private void StretchLastColumn()
        {
            if (Columns.Count == 0) return;
            int others = 0;
            for (int i = 0; i < Columns.Count - 1; i++)
                others += Columns[i].Width;
            int fill = ClientSize.Width - others - Px(4);
            if (fill > Px(40))
                Columns[Columns.Count - 1].Width = fill;
        }
    }
}
