using MauiMaterial.Controls.Switch;
using MauiMaterial.Core;
using MauiMaterial.Helper;

namespace MauiMaterial.Controls;

public class MaterialSwitch : GraphicsView, IMaterialSwitch
{
    internal const float _defaultTrackWidth  = 52f;
    internal const float _defaultTrackHeight = 32f;

    // M3: 300ms toggle duration.
    const uint ThumbAnimationDuration = 300;

    // HaloInset = (48dp touch target − 32dp track) / 2 = 8dp.
    // Gives view 68×48dp with 4dp AA buffer around the 40dp halo on all sides.
    internal const float HaloInset = 8f;

    // Raw linear position: 0 = OFF, 1 = ON.
    // Easing is applied per-property inside Draw() to match Flutter's approach
    // of using separate curves for position (easeOutBack) and color (easeOut).
    internal float _rawPosition;

    // Tracks the direction of the last animation — required for the direction-aware
    // thumb shape morph (forward vs reverse TweenSequence branches).
    bool _isGoingToOn;

    // Hover halo opacity (0..1).
    internal float _haloOpacity;

    // Press state layer progress (0..1). Expands thumb to 28dp while > 0.
    internal float _pressProgress;

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(bool), typeof(MaterialSwitch), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var sw = (MaterialSwitch)bindable;
                sw.AnimateThumbToCheckedState((bool)newValue);
                sw.ValueChanged?.Invoke(sw, new SwitchValueChangedEventArgs((bool)oldValue, (bool)newValue));
                SemanticScreenReader.Announce((bool)newValue ? "Switch on" : "Switch off");
            });

    public bool Value
    {
        get => (bool)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler<SwitchValueChangedEventArgs>? ValueChanged;

    public MaterialSwitch()
    {
        Drawable = new SwitchDrawable(this);
        SetupGestureRecognizers();
        WidthRequest  = _defaultTrackWidth  + HaloInset * 2;  // 68dp
        HeightRequest = _defaultTrackHeight + HaloInset * 2;  // 48dp (M3 touch target)
        _rawPosition  = Value ? 1f : 0f;
        SemanticProperties.SetDescription(this, "Toggle switch");
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
            this.AbortAnimation("ThumbPosition");
            this.AbortAnimation("HaloOpacity");
            this.AbortAnimation("PressState");
            Application.Current?.RequestedThemeChanged -= OnAppThemeChanged;
        }
        base.OnHandlerChanging(args);
    }

    void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

    void AnimateThumbToCheckedState(bool toOn)
    {
        _isGoingToOn = toOn;
        float target = toOn ? 1f : 0f;

        this.AbortAnimation("ThumbPosition");

        if (Handler is null)
        {
            _rawPosition = target;
            return;
        }

        float start = _rawPosition;

        // Animate linearly — EaseOutBack (position) and EaseOut (color)
        // are applied in Draw(), matching Flutter's independent-curve approach.
        var animation = new Animation(v =>
        {
            _rawPosition = (float)v;
            Invalidate();
        }, start, target, Easing.Linear);

        animation.Commit(this, "ThumbPosition", 16, ThumbAnimationDuration,
            finished: (_, cancelled) =>
            {
                if (!cancelled) { _rawPosition = target; Invalidate(); }
            });
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        => new Size(_defaultTrackWidth + HaloInset * 2, _defaultTrackHeight + HaloInset * 2);

    void SetupGestureRecognizers()
    {
        GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => Value = !Value)
        });

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered  += (_, _) => AnimateHalo(true);
        pointer.PointerExited   += (_, _) => { AnimateHalo(false); AnimatePress(false); };
        pointer.PointerPressed  += (_, _) => AnimatePress(true);
        pointer.PointerReleased += (_, _) => AnimatePress(false);
        GestureRecognizers.Add(pointer);
    }

    void AnimateHalo(bool show)
    {
        this.AbortAnimation("HaloOpacity");
        float target = show ? 1f : 0f;
        var anim = new Animation(v => { _haloOpacity = (float)v; Invalidate(); },
            _haloOpacity, target, Easing.CubicOut);
        anim.Commit(this, "HaloOpacity", 16, 150,
            finished: (_, cancelled) =>
            {
                if (!cancelled) { _haloOpacity = target; Invalidate(); }
            });
    }

    void AnimatePress(bool pressing)
    {
        this.AbortAnimation("PressState");
        float target = pressing ? 1f : 0f;
        var anim = new Animation(v => { _pressProgress = (float)v; Invalidate(); },
            _pressProgress, target, Easing.CubicOut);
        anim.Commit(this, "PressState", 16, 100,
            finished: (_, cancelled) =>
            {
                if (!cancelled) { _pressProgress = target; Invalidate(); }
            });
    }

    // ─────────────────────────────────────────────────────────────────────────

    class SwitchDrawable : IDrawable
    {
        readonly MaterialSwitch _switch;

        const float TrackOutlineWidth = 2f;

        // M3 thumb diameter constants (from _SwitchConfigM3).
        const float ThumbOffD   = 16f;  // ø16 when OFF
        const float ThumbOnD    = 24f;  // ø24 when ON
        const float ThumbPressD = 28f;  // ø28 while pressed

        // Transitional pill dimensions at the mid-point of the toggle animation.
        const float TransitW = 34f;
        const float TransitH = 22f;

        public SwitchDrawable(MaterialSwitch sw) => _switch = sw;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            Color trackOffColor = isDark ? MaterialColors.M3SurfaceContainerHighestDark : MaterialColors.M3SurfaceContainerHighestLight;
            Color trackOnColor  = isDark ? MaterialColors.M3PrimaryDark                 : MaterialColors.M3PrimaryLight;
            Color outlineColor  = isDark ? MaterialColors.M3OutlineDark                 : MaterialColors.M3OutlineLight;
            Color thumbOffColor = isDark ? MaterialColors.M3OutlineDark                 : MaterialColors.M3OutlineLight;
            Color thumbOnColor  = isDark ? MaterialColors.M3OnPrimaryDark               : MaterialColors.M3OnPrimaryLight;

            float rawT = Math.Clamp(_switch._rawPosition, 0f, 1f);

            // Color uses a faster easeOut curve (matches Flutter's _colorAnimation).
            float colorT = (float)EaseOut(rawT);

            // Position uses easeOutBack — the springy overshoot is the signature M3 feel.
            // Allow slight overshoot beyond [0,1]; the thumb can protrude past the track
            // endpoints briefly, which is intentional (Flutter does the same).
            float posT = (float)EaseOutBack(rawT);

            // ── Track ─────────────────────────────────────────────────────────
            float hi     = HaloInset;
            float trackX = dirtyRect.X + hi;
            float trackY = dirtyRect.Y + hi;
            float trackW = _defaultTrackWidth;
            float trackH = _defaultTrackHeight;
            float trackR = trackH / 2f;

            canvas.FillColor = InterpolateColor(trackOffColor, trackOnColor, colorT);
            canvas.FillRoundedRectangle(trackX, trackY, trackW, trackH, trackR);

            // Outline fades out as state transitions to ON (no outline when selected).
            if (colorT < 1f)
            {
                canvas.StrokeColor = new Color(
                    outlineColor.Red, outlineColor.Green, outlineColor.Blue,
                    outlineColor.Alpha * (1f - colorT));
                canvas.StrokeSize = TrackOutlineWidth;
                canvas.DrawRoundedRectangle(trackX, trackY, trackW, trackH, trackR);
            }

            // ── Thumb center (easeOutBack for horizontal travel) ──────────────
            // Track inner length = trackWidth − trackHeight = 52 − 32 = 20dp.
            // Center travels from trackHeight/2 (16dp) to trackWidth−trackHeight/2 (36dp).
            float innerStart  = trackH / 2f;          // 16dp from track left
            float innerLength = trackW - trackH;       // 20dp travel distance
            float thumbCX = trackX + innerStart + innerLength * posT;
            float thumbCY = trackY + trackH / 2f;

            // ── Thumb size (3-segment TweenSequence, direction-aware) ─────────
            (float thumbW, float thumbH) = ComputeThumbSize(rawT, _switch._isGoingToOn);

            // Pressed state blends thumb size toward 28dp.
            float pressT = Math.Clamp(_switch._pressProgress, 0f, 1f);
            if (pressT > 0f)
            {
                thumbW = thumbW + (ThumbPressD - thumbW) * pressT;
                thumbH = thumbH + (ThumbPressD - thumbH) * pressT;
            }

            // ── Halo (state layer, 40dp diameter) ─────────────────────────────
            float haloOpacity = Math.Clamp(_switch._haloOpacity, 0f, 1f);
            if (haloOpacity > 0f)
            {
                Color haloBase = isDark ? MaterialColors.M3OnSurfaceVariantDark : MaterialColors.M3OnSurfaceVariantLight;
                const float HaloDiameter = 40f;
                canvas.FillColor = new Color(haloBase.Red, haloBase.Green, haloBase.Blue, 0.12f * haloOpacity);
                canvas.FillEllipse(thumbCX - HaloDiameter / 2f, thumbCY - HaloDiameter / 2f, HaloDiameter, HaloDiameter);
            }

            // ── Thumb (stadium shape: cornerRadius = height/2) ────────────────
            canvas.FillColor = InterpolateColor(thumbOffColor, thumbOnColor, colorT);
            canvas.FillRoundedRectangle(
                thumbCX - thumbW / 2f, thumbCY - thumbH / 2f,
                thumbW, thumbH,
                thumbH / 2f);  // StadiumBorder equivalent
        }

        // Mirrors Flutter's _SwitchPainter.thumbSizeAnimation() TweenSequence.
        //
        // Forward (OFF → ON)  weights: 11% | 72% | 17%
        //   ø16 → 34×22 pill (fast) → ø24 (slow) → hold
        //
        // Reverse (ON → OFF)  weights: 17% | 72% | 11%
        //   hold → ø16 → 34×22 pill (slow) → ø24 (fast)
        //   Evaluated at the same rawT (decreasing 1→0 during this branch).
        static (float w, float h) ComputeThumbSize(float rawT, bool isGoingToOn)
        {
            if (isGoingToOn)
            {
                const float seg1 = 0.11f;
                const float seg2 = 0.83f;  // 0.11 + 0.72

                if (rawT <= seg1)
                {
                    float s = CubicBezier(rawT / seg1, 0.31f, 0.00f, 0.56f, 1.00f);
                    return (Lerp(ThumbOffD, TransitW, s), Lerp(ThumbOffD, TransitH, s));
                }
                if (rawT <= seg2)
                {
                    float s = CubicBezier((rawT - seg1) / (seg2 - seg1), 0.20f, 0.00f, 0.00f, 1.00f);
                    return (Lerp(TransitW, ThumbOnD, s), Lerp(TransitH, ThumbOnD, s));
                }
                return (ThumbOnD, ThumbOnD);
            }
            else
            {
                const float seg1 = 0.17f;
                const float seg2 = 0.89f;  // 0.17 + 0.72

                if (rawT >= seg2)
                {
                    float s = CubicBezier((rawT - seg2) / (1f - seg2), 0.31f, 0.00f, 0.56f, 1.00f);
                    return (Lerp(TransitW, ThumbOnD, s), Lerp(TransitH, ThumbOnD, s));
                }
                if (rawT >= seg1)
                {
                    float s = CubicBezier((rawT - seg1) / (seg2 - seg1), 0.20f, 0.00f, 0.00f, 1.00f);
                    return (Lerp(ThumbOffD, TransitW, s), Lerp(ThumbOffD, TransitH, s));
                }
                return (ThumbOffD, ThumbOffD);
            }
        }

        // EaseOutBack — matches Flutter's Curves.easeOutBack.
        // Produces a slight overshoot past the target before settling.
        static double EaseOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1.0;
            return 1.0 + c3 * Math.Pow(t - 1.0, 3) + c1 * Math.Pow(t - 1.0, 2);
        }

        // Quadratic EaseOut — matches Flutter's _colorAnimation (easeOut curve).
        static double EaseOut(double t) => 1.0 - Math.Pow(1.0 - t, 2.0);

        // CSS cubic-bezier(x1, y1, x2, y2) via binary search on x → evaluate y.
        static float CubicBezier(float t, float x1, float y1, float x2, float y2)
        {
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 12; i++)
            {
                float mid = (lo + hi) / 2f;
                float bx  = BezierComp(mid, x1, x2);
                if (MathF.Abs(bx - t) < 0.0001f) return BezierComp(mid, y1, y2);
                if (bx < t) lo = mid; else hi = mid;
            }
            return BezierComp((lo + hi) / 2f, y1, y2);
        }

        // Cubic bezier component: B(t) = 3(1-t)²t·p1 + 3(1-t)t²·p2 + t³.
        static float BezierComp(float t, float p1, float p2)
        {
            float mt = 1f - t;
            return 3f * mt * mt * t * p1 + 3f * mt * t * t * p2 + t * t * t;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static Color InterpolateColor(Color from, Color to, float t) =>
            new Color(
                from.Red   + (to.Red   - from.Red)   * t,
                from.Green + (to.Green - from.Green) * t,
                from.Blue  + (to.Blue  - from.Blue)  * t,
                from.Alpha + (to.Alpha - from.Alpha) * t);
    }
}