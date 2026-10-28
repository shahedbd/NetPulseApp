using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Pages.Settings;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Narrow-window mode (Settings_UI_Redesign_Plan.md §3.10): below
    /// <see cref="NarrowWidth"/> the category column hides and a strip with
    /// the search box and a category dropdown takes its place.
    /// </summary>
    public partial class SettingsPageControl
    {
        /// <summary>Below this page width the category column folds into a dropdown (plan §3.10).</summary>
        private const int NarrowWidth = 720;

        private Panel _compactBar;
        private ModernDropDown _cmbCategory;
        private bool? _narrow;

        /// <summary>Narrow-window replacement for the column: search box + category dropdown in one strip.</summary>
        private Panel BuildCompactBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = Scale(44), Visible = false, BackColor = Color.Transparent };
            _cmbCategory = new ModernDropDown { Width = Scale(180), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            foreach (var name in CategoryNames) _cmbCategory.Items.Add(name);
            _cmbCategory.AccessibleName = "Settings category";
            _cmbCategory.ApplyTheme();
            _cmbCategory.SelectionChanged += (s, e) =>
            {
                if (_suppressSearch || _cmbCategory.SelectedIndex < 0 || _cmbCategory.SelectedIndex == _current && _shown is SettingsCategory) return;
                OpenCategoryFromNav(_cmbCategory.SelectedIndex);
            };
            bar.Controls.Add(_cmbCategory);
            bar.Resize += (s, e) =>
            {
                _cmbCategory.Location = new Point(bar.Width - _cmbCategory.Width, Scale(6));
                if (_txtSearch.Parent == bar)
                    _txtSearch.Width = Math.Max(Scale(120), _cmbCategory.Left - Scale(12));
            };
            return bar;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyNarrowMode();
        }

        /// <summary>
        /// Swaps between the category column and the compact bar, moving the
        /// one search box between them so its text and focus survive.
        /// </summary>
        private void ApplyNarrowMode()
        {
            if (_nav == null || _compactBar == null) return;
            bool narrow = Width > 0 && Width < Scale(NarrowWidth);
            if (_narrow == narrow) return;
            _narrow = narrow;

            SuspendLayout();
            _nav.Visible = _navDivider.Visible = !narrow;
            _compactBar.Visible = narrow;

            if (narrow)
            {
                _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                _txtSearch.Location = new Point(0, Scale(9));
                _compactBar.Controls.Add(_txtSearch);
                _txtSearch.Width = Math.Max(Scale(120), _compactBar.Width - _cmbCategory.Width - Scale(12));
            }
            else
            {
                _txtSearch.Location = new Point(_nav.Padding.Left, _nav.Padding.Top);
                _txtSearch.Width = _nav.Width - _nav.Padding.Horizontal;
                _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _nav.Controls.Add(_txtSearch);
            }
            ResumeLayout(true);
        }
    }
}
