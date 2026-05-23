using MauiMaterial.Controls.CheckBox;
using MauiMaterial.Core;
using MauiMaterial.Helper;

namespace MauiMaterial.Controls;

public class MaterialCheckBox : GraphicsView, IMaterialCheckBox
{
    internal const float BoxSize = 18f;
    internal const float BoxCornerRadius = 2f;
    // M3 spec: touch target = 48dp, halo = 40dp, box = 18dp.
    // HaloInset = (TouchTarget[48] - BoxSize[18]) / 2 = 15dp.
    // Halo (r=20) center at 24dp → edges at 4dp and 44dp, leaving 4dp AA buffer inside 48dp view.
    internal const float HaloInset = 15f;
    const float OutlineWidth = 2f;
    const uint CheckAnimationDuration = 200;

    // 0 = unchecked, 1 = checked — drives fill, outline fade, and checkmark draw.
    internal float _checkProgress;
    // 0 = no halo, 1 = full halo (pointer hover).
    internal float _haloOpacity;

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(bool), typeof(MaterialCheckBox), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var cb = (MaterialCheckBox)bindable;
                cb.AnimateCheck((bool)newValue);
                cb.ValueChanged?.Invoke(cb, new CheckBoxValueChangedEventArgs((bool)oldValue, (bool)newValue));
                SemanticScreenReader.Announce((bool)newValue ? "Checkbox checked" : "Checkbox unchecked");
            });

    #region Property

    public bool Value
    {
        get => (bool)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    #endregion

    #region Event

    public event EventHandler<CheckBoxValueChangedEventArgs>? ValueChanged;

    #endregion

    public MaterialCheckBox()
    {
        Drawable = new CheckBoxDrawable(this);
        SetupGestureRecognizers();
        WidthRequest  = BoxSize + 2 * HaloInset;
        HeightRequest = BoxSize + 2 * HaloInset;
        _checkProgress = Value ? 1f : 0f;
        SemanticProperties.SetDescription(this, "Checkbox");
        SemanticProperties.SetHint(this, "Double tap to toggle");
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null && Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnAppThemeChanged;
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            this.AbortAnimation("CheckProgress");
            this.AbortAnimation("HaloOpacity");
            if (Application.Current is not null)
                Application.Current.RequestedThemeChanged -= OnAppThemeChanged;
        }
        base.OnHandlerChanging(args);
    }

    void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

    void AnimateCheck(bool newCheckedState)
    {
        float target = newCheckedState ? 1f : 0f;

        this.AbortAnimation("CheckProgress");

        if (Handler is null)
        {
            _checkProgress = target;
            return;
        }

        float start = _checkProgress;
        var animation = new Animation(v =>
        {
            _checkProgress = (float)v;
            Invalidate();
        }, start, target, Easing.CubicOut);

        animation.Commit(this, "CheckProgress", 16, CheckAnimationDuration, finished: (v, cancelled) =>
        {
            if (!cancelled)
            {
                _checkProgress = target;
                Invalidate();
            }
        });
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        double size = BoxSize + 2 * HaloInset;
        var w = double.IsFinite(widthConstraint)  ? Math.Min(widthConstraint,  size) : size;
        var h = double.IsFinite(heightConstraint) ? Math.Min(heightConstraint, size) : size;
        return new Size(w, h);
    }

    void SetupGestureRecognizers()
    {
        GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => Value = !Value)
        });

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => AnimateHalo(show: true);
        pointer.PointerExited  += (_, _) => AnimateHalo(show: false);
        GestureRecognizers.Add(pointer);
    }

    void AnimateHalo(bool show)
    {
        this.AbortAnimation("HaloOpacity");
        float target = show ? 1f : 0f;
        var animation = new Animation(v =>
        {
            _haloOpacity = (float)v;
            Invalidate();
        }, _haloOpacity, target, Easing.CubicOut);

        animation.Commit(this, "HaloOpacity", 16, 150, finished: (v, cancelled) =>
        {
            if (!cancelled)
            {
                _haloOpacity = target;
                Invalidate();
            }
        });
    }

    class CheckBoxDrawable : IDrawable
    {
        readonly MaterialCheckBox _checkbox;
        const float OutlineWidth = 2f;
        // Checkmark path points relative to box top-left (box is 18×18dp).
        // Two-leg path: Start → Knee → End
        const float CkStartX = 3f,  CkStartY = 9f;
        const float CkKneeX  = 7f,  CkKneeY  = 13f;
        const float CkEndX   = 15f, CkEndY   = 5f;
        // Leg1 ≈ 33% of total path length, leg2 ≈ 67%.
        const float Leg1Fraction = 1f / 3f;

        public CheckBoxDrawable(MaterialCheckBox checkbox) => _checkbox = checkbox;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            Color primaryColor        = isDark ? MaterialColors.M3PrimaryDark              : MaterialColors.M3PrimaryLight;
            Color onPrimaryColor      = isDark ? MaterialColors.M3OnPrimaryDark             : MaterialColors.M3OnPrimaryLight;
            // M3 spec: unselected outline uses OnSurfaceVariant, NOT Outline.
            Color onSurfaceVariant    = isDark ? MaterialColors.M3OnSurfaceVariantDark      : MaterialColors.M3OnSurfaceVariantLight;
            // M3 spec: hover state layer is OnSurface (unchecked) → Primary (checked) at 8% opacity.
            Color haloUncheckedColor  = isDark ? MaterialColors.M3OnSurfaceDark             : MaterialColors.M3OnSurfaceLight;

            float hi = MaterialCheckBox.HaloInset;
            float t  = Math.Clamp(_checkbox._checkProgress, 0f, 1f);

            float boxX = dirtyRect.X + hi;
            float boxY = dirtyRect.Y + hi;
            float boxS = MaterialCheckBox.BoxSize;
            float cr   = MaterialCheckBox.BoxCornerRadius;

            // 1. Halo (state layer) — drawn first so box renders on top.
            float haloOpacity = Math.Clamp(_checkbox._haloOpacity, 0f, 1f);
            if (haloOpacity > 0f)
            {
                // M3 spec: hover state layer OnSurface (unchecked) → Primary (checked) at 8%.
                Color haloBase = InterpolateColor(haloUncheckedColor, primaryColor, t);
                const float HaloDiameter = 40f;
                float cx = boxX + boxS / 2f;
                float cy = boxY + boxS / 2f;
                canvas.FillColor = new Color(haloBase.Red, haloBase.Green, haloBase.Blue, 0.08f * haloOpacity);
                canvas.FillEllipse(cx - HaloDiameter / 2f, cy - HaloDiameter / 2f, HaloDiameter, HaloDiameter);
            }

            // 2. Box fill — Primary color, fades in as t → 1.
            if (t > 0f)
            {
                canvas.FillColor = new Color(primaryColor.Red, primaryColor.Green, primaryColor.Blue, t);
                canvas.FillRoundedRectangle(boxX, boxY, boxS, boxS, cr);
            }

            // 3. Box outline — OnSurfaceVariant (per M3 spec), fades out as t → 1.
            if (t < 1f)
            {
                float inset = OutlineWidth / 2f;
                canvas.StrokeColor = new Color(onSurfaceVariant.Red, onSurfaceVariant.Green, onSurfaceVariant.Blue, 1f - t);
                canvas.StrokeSize  = OutlineWidth;
                canvas.DrawRoundedRectangle(boxX + inset, boxY + inset, boxS - OutlineWidth, boxS - OutlineWidth, cr);
            }

            // 4. Checkmark — drawn progressively as t → 1.
            if (t > 0f)
            {
                canvas.StrokeColor    = new Color(onPrimaryColor.Red, onPrimaryColor.Green, onPrimaryColor.Blue, t);
                canvas.StrokeSize     = 2f;
                canvas.StrokeLineCap  = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;

                float x0 = boxX + CkStartX, y0 = boxY + CkStartY;
                float x1 = boxX + CkKneeX,  y1 = boxY + CkKneeY;
                float x2 = boxX + CkEndX,   y2 = boxY + CkEndY;

                var path = new PathF();
                path.MoveTo(x0, y0);

                if (t <= Leg1Fraction)
                {
                    // Animating first leg: start → knee.
                    float seg = t / Leg1Fraction;
                    path.LineTo(x0 + (x1 - x0) * seg, y0 + (y1 - y0) * seg);
                }
                else
                {
                    // First leg complete; animating second leg: knee → end.
                    float seg = (t - Leg1Fraction) / (1f - Leg1Fraction);
                    path.LineTo(x1, y1);
                    path.LineTo(x1 + (x2 - x1) * seg, y1 + (y2 - y1) * seg);
                }

                canvas.DrawPath(path);
            }
        }

        static Color InterpolateColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                a.Red   + (b.Red   - a.Red)   * t,
                a.Green + (b.Green - a.Green) * t,
                a.Blue  + (b.Blue  - a.Blue)  * t);
        }
    }
}
