using MauiMaterial.Controls.Switch;
using MauiMaterial.Core;
using MauiMaterial.Helper;

namespace MauiMaterial.Controls;

public class MaterialSwitch : GraphicsView, IMaterialSwitch
{
    internal const float _defaultTrackWidth = 52f;
    internal const float _defaultTrackHeight = 32f;
    const uint ThumbAnimationDuration = 250;

    // Animated thumb position: 0 = off (left), 1 = on (right).
    internal float _thumbPosition;

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(bool), typeof(MaterialSwitch), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var materialSwitch = (MaterialSwitch)bindable;
                materialSwitch.AnimateThumbToCheckedState((bool)newValue);
                materialSwitch.ValueChanged?.Invoke(materialSwitch, new SwitchValueChangedEventArgs((bool)oldValue, (bool)newValue));
                SemanticScreenReader.Announce((bool)newValue ? "Switch on" : "Switch off");
            });

    #region  Property

    public bool Value
    {
        get => (bool)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }


    #endregion

    #region Event

    public event EventHandler<SwitchValueChangedEventArgs>? ValueChanged;

    #endregion

    public MaterialSwitch()
    {
        Drawable = new SwitchDrawable(this);
        SetupGestureRecognizers();
        WidthRequest = _defaultTrackWidth;
        HeightRequest = _defaultTrackHeight;
        _thumbPosition = Value ? 1f : 0f;
        SemanticProperties.SetDescription(this, "Toggle switch");
        SemanticProperties.SetHint(this, "Double tap to toggle");
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            this.AbortAnimation("ThumbPosition");
        }

        base.OnHandlerChanging(args);
    }

    void AnimateThumbToCheckedState(bool newCheckedState)
    {
        float target = newCheckedState ? 1f : 0f;

        // Cancel any in-progress animation.
        this.AbortAnimation("ThumbPosition");

        if (Handler is null)
        {
            // Not yet attached; snap to target.
            _thumbPosition = target;
            return;
        }

        float start = _thumbPosition;
        var animation = new Animation(v =>
        {
            _thumbPosition = (float)v;
            Invalidate();
        }, start, target, Easing.CubicOut);

        animation.Commit(this, "ThumbPosition", 16, ThumbAnimationDuration, finished: (v, cancelled) =>
        {
            if (!cancelled)
            {
                _thumbPosition = target;
                Invalidate();
            }
        });
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var width = double.IsFinite(widthConstraint) ? Math.Min(widthConstraint, _defaultTrackWidth) : _defaultTrackWidth;
        var height = double.IsFinite(heightConstraint) ? Math.Min(heightConstraint, _defaultTrackHeight) : _defaultTrackHeight;
        return new Size(width, height);
    }

    void SetupGestureRecognizers()
    {
        GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                Value = !Value;
            })
        });
    }

    class SwitchDrawable : IDrawable
    {
        readonly MaterialSwitch _switch;
        const float TrackOutlineWidth = 2f;
        const float SelectedThumbDiameter = 24f;
        const float UnselectedThumbDiameter = 16f;

        public SwitchDrawable(MaterialSwitch customSwitch)
        {
            _switch = customSwitch;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float inset = TrackOutlineWidth / 2f;
            float trackX = dirtyRect.X + inset;
            float trackY = dirtyRect.Y + inset;
            float trackW = dirtyRect.Width - TrackOutlineWidth;
            float trackH = dirtyRect.Height - TrackOutlineWidth;
            float trackRadius = trackH / 2f;

            float t = Math.Clamp(_switch._thumbPosition, 0f, 1f);

            // Interpolate track fill: surface-container-highest -> primary.
            Color trackFill = InterpolateColor(
                MaterialColors.M3SurfaceContainerHighestLight,
                MaterialColors.M3PrimaryLight,
                t);
            canvas.FillColor = trackFill;
            canvas.FillRoundedRectangle(trackX, trackY, trackW, trackH, trackRadius);

            // Outline fades out as we move to the on state.
            if (t < 1f)
            {
                var outline = MaterialColors.M3OutlineLight;
                canvas.StrokeColor = new Color(outline.Red, outline.Green, outline.Blue, outline.Alpha * (1f - t));
                canvas.StrokeSize = TrackOutlineWidth;
                canvas.DrawRoundedRectangle(trackX, trackY, trackW, trackH, trackRadius);
            }

            // Thumb size grows from unselected (16dp) to selected (24dp).
            float thumbD = UnselectedThumbDiameter + (SelectedThumbDiameter - UnselectedThumbDiameter) * t;
            float padding = (dirtyRect.Height - thumbD) / 2f;

            // Travel: left edge (unselected) -> right edge (selected).
            float thumbXOff = dirtyRect.X + padding;
            float thumbXOn = dirtyRect.X + dirtyRect.Width - thumbD - padding;
            float thumbX = thumbXOff + (thumbXOn - thumbXOff) * t;
            float thumbY = dirtyRect.Y + padding;

            // Thumb color: outline -> on-primary.
            canvas.FillColor = InterpolateColor(
                MaterialColors.M3OutlineLight,
                MaterialColors.M3OnPrimaryLight,
                t);
            canvas.FillEllipse(thumbX, thumbY, thumbD, thumbD);
        }

        static Color InterpolateColor(Color from, Color to, float t)
        {
            return new Color(
                from.Red + (to.Red - from.Red) * t,
                from.Green + (to.Green - from.Green) * t,
                from.Blue + (to.Blue - from.Blue) * t,
                from.Alpha + (to.Alpha - from.Alpha) * t);
        }
    }
}