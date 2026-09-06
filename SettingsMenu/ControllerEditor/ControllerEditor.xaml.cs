using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace OverjoyedVersion3;

public partial class ControllerEditor : ContentView
{
    private Controller? _selectedController;

    private readonly DialOptionsPanel _dialOptionsPanel;
    private readonly JoystickOptionsPanel _joystickOptionsPanel;

    private AbsoluteLayout? _controllerVisual;
    private AbsoluteLayout? _scalingWrapper;
    private EditorControllerView? _controllerView;
    private Icon? _controllerIcon;
    private double _scale;

    private WidgetType _dragWidgetType;
    private View? _dragPreview;
    private View _dragRoot;
    private readonly Dictionary<WidgetType, View> _dragPreviews = new();
    private readonly List<(Border Card, WidgetType Type)> _widgetCards = new();

    public ControllerEditor()
    {
        InitializeComponent();
        _dialOptionsPanel = new(this);
        _joystickOptionsPanel = new(this);
        _dragRoot = Content;
        SettingsManager.PrimaryColorChanged += OnPrimaryColorChanged;
        SettingsManager.SecondaryColorChanged += OnSecondaryColorChanged;
        SettingsManager.FontColorChanged += (_, _) => RefreshTextColors();
        IconManager.IconsRecolored += (_, _) => RefreshIcons();
        _controllerIcon = IconManager.GetIcon("gamepad");

        foreach (var (name, controller) in ControllerManager.Controllers)
        {
            ControllerListStack.Children.Add(CreateControllerFile(controller.Name,
                () => OnControllerFileClicked(controller)));
        }

        LoadWidgetDrawer();
    }

    private void RefreshTextColors()
    {
        foreach (var item in ControllerListStack.Children.OfType<VerticalStackLayout>())
        {
            if (item.Children.OfType<Label>().FirstOrDefault() is { } label) label.TextColor = SettingsManager.FontColor;
        }
        foreach (var (card, _) in _widgetCards)
        {
            if (card.Content is not VerticalStackLayout content) continue;
            if (content.Children.OfType<Label>().FirstOrDefault() is { } label) label.TextColor = SettingsManager.FontColor;
        }
    }

    private void RefreshIcons()
    {
        _controllerIcon = IconManager.GetIcon("gamepad");
        foreach (var item in ControllerListStack.Children.OfType<VerticalStackLayout>())
        {
            if (item.Children.OfType<Image>().FirstOrDefault() is { } image) image.Source = _controllerIcon;
        }

        foreach (var (card, type) in _widgetCards)
        {
            if (card.Content is not VerticalStackLayout content) continue;
            if (content.Children.OfType<Image>().FirstOrDefault() is { } image)
                image.Source = IconManager.GetIcon(type == WidgetType.Dial ? "dial" : "joystick");
        }
    }
    
    public event EventHandler? CloseRequested;

    public void Open()
    {
#if WINDOWS
        NativeMethods.SetNoActivate(false);
#endif
        if (ControllerManager.ActiveController != null)
        {
            _selectedController = ControllerManager.ActiveController;
            HighlightControllerFile();
            LoadController(_selectedController);
        }
    }
    public void Close()
    {
#if WINDOWS
        NativeMethods.SetNoActivate(true);
#endif
    }

    private void OnPrimaryColorChanged(object? sender, Color color)
    {
        if (_controllerVisual != null)
        {
            _controllerVisual.BackgroundColor = color;
        }
    }

    private void OnSecondaryColorChanged(object? sender, Color color)
    {
        foreach (var card in WidgetDrawer.Children.OfType<Border>())
        {
            card.BackgroundColor = WidgetCardBackground;
            card.Stroke = new SolidColorBrush(color);
        }
        foreach (var card in _dragPreviews.Values.OfType<Border>())
        {
            card.BackgroundColor = WidgetCardBackground;
            card.Stroke = new SolidColorBrush(color);
        }

        HighlightControllerFile();
    }

    private static Color WidgetCardBackground => SettingsManager.WidgetHighlightColor;
    private static Color FileHighlightBackground => SettingsManager.Lighten(SettingsManager.SecondaryColor, 0.18f);

    private void OnControllerCanvasSizeChanged(object? sender, EventArgs e)
    {
        if (_selectedController != null)
        {
            LoadController(_selectedController);
        }
    }

    private void LoadController(Controller controller)
    {
        InspectorPanel.IsVisible = false;
        InspectorPanel.Content = null;

        // Get the dimensions of a controller in MainPage.xaml.
        double liveControllerWidth = Controller.GetLiveControllerWidth();
        double liveControllerHeight = Controller.GetLiveControllerHeight();

        // Get the available height and width for the controller in ControllerEditor.xaml. 
        double availableWidth = ControllerCanvasScrollView.Width;
        double availableHeight = ControllerCanvasScrollView.Height;

        if (availableWidth <= 0 || availableHeight <= 0 || liveControllerWidth <= 0 ||
            liveControllerHeight <= 0)
        {
            return;
        }

        _scale = Math.Min(availableWidth / liveControllerWidth, availableHeight / liveControllerHeight);

        if (_controllerVisual == null)
        {
            _controllerVisual = new AbsoluteLayout
            {
                WidthRequest = liveControllerWidth,
                HeightRequest = liveControllerHeight,
                BackgroundColor = SettingsManager.PrimaryColor,
                AnchorX = 0,
                AnchorY = 0,
                Scale = _scale,
            };

            // When you apply scale to controllerVisual, it visually shrinks but the layout system still
            // uses the original HeightRequest and WidthRequest. If you put controllerVisual directly in
            // ControllerContainer, MAUI will try to center it using the original size request, not the
            // scaled visual size, and it would be off. So we create a wrapper with a HeightRequest and
            // WidthRequest matching the visual size of controllerVisual.

            // Note: If you try to remove the scaling wrapper and just set the HeightRequest and
            // WidthRequest of controllerVisual to the scaled size after placing the widgets it won't work.
            // Setting these values will apply another scale transformation: width = w * scale * scale.
            // It's jank but it works, so don't remove it.

            _scalingWrapper = new AbsoluteLayout
            {
                IsClippedToBounds = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = liveControllerWidth * _scale,
                HeightRequest = liveControllerHeight * _scale,
            };
            AbsoluteLayout.SetLayoutBounds(_controllerVisual, new Rect(0, 0, liveControllerWidth, liveControllerHeight));
            AbsoluteLayout.SetLayoutFlags(_controllerVisual, AbsoluteLayoutFlags.None);
            _scalingWrapper.Children.Add(_controllerVisual);

            ControllerContainer.Children.Clear();
            ControllerContainer.Add(_scalingWrapper);
        }
        else
        {
            // The canvas was resized after the controller was already loaded — rescale in place.
            _controllerVisual.Scale = _scale;
            _scalingWrapper!.WidthRequest = liveControllerWidth * _scale;
            _scalingWrapper.HeightRequest = liveControllerHeight * _scale;
        }

        if (_controllerView == null)
        {
            _controllerView = new EditorControllerView(controller);
            _controllerView.WidgetSelected += ShowOptionsPanel;
            AbsoluteLayout.SetLayoutBounds(_controllerView, new Rect(0, 0, liveControllerWidth, liveControllerHeight));
            AbsoluteLayout.SetLayoutFlags(_controllerView, AbsoluteLayoutFlags.None);
            _controllerVisual.Children.Add(_controllerView);
        }
        else
        {
            _controllerView.DisplayController(controller);
        }

        _controllerView.WidthRequest = liveControllerWidth;
        _controllerView.HeightRequest = liveControllerHeight;

        // Restore the last-selected widget's options panel for this controller, if any.
        _controllerView.RestoreLastSelectedWidget();
    }
    private void ShowOptionsPanel(Widget widget, string? slotId)
    {
        if (widget is Dial)
        {
            _dialOptionsPanel.Update(slotId ?? string.Empty, widget);
            InspectorPanel.IsVisible = true;
            InspectorPanel.Content = _dialOptionsPanel;
        }
        else if (widget is Joystick)
        {
            _joystickOptionsPanel.Update(slotId ?? string.Empty, widget);
            InspectorPanel.IsVisible = true;
            InspectorPanel.Content = _joystickOptionsPanel;
        }
    }
    
    public void InvalidateCanvas() => _controllerView?.Invalidate();

    private void LoadWidgetDrawer()
    {
        // Create the stationary widget cards.
        View widgetCard = CreateWidgetCard(WidgetType.Dial);
        widgetCard.Margin = new Thickness(0, 0, 8, 0);
        WidgetDrawer.Add(widgetCard);

        widgetCard = CreateWidgetCard(WidgetType.Joystick);
        widgetCard.Margin = new Thickness(0, 0, 8, 0);
        WidgetDrawer.Add(widgetCard);

        // Create the drag preview widget cards.
        _dragPreviews[WidgetType.Dial] = CreateWidgetCard(WidgetType.Dial);
        _dragPreviews[WidgetType.Joystick] = CreateWidgetCard(WidgetType.Joystick);
    }
    private View CreateControllerFile(string name, Action onTap)
    {
        var image = new Image
        {
            Source = _controllerIcon,
            WidthRequest = 56,
            HeightRequest = 56,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
        };
        var label = new Label
        {
            Text = name,
            FontSize = 12,
            TextColor = SettingsManager.FontColor,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        var item = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(4, 6),
            BackgroundColor = Colors.Transparent,
            Children = { image, label },
        };
        item.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });
        return item;
    }
    private View CreateWidgetCard(WidgetType widgetType)
    {
        var iconName = widgetType switch
        {
            WidgetType.Dial => "dial",
            WidgetType.Joystick => "joystick",
            _ => throw new ArgumentOutOfRangeException(nameof(widgetType))
        };
        var image = new Image { WidthRequest = 140, HeightRequest = 140, Aspect = Aspect.AspectFit };
        image.Source = IconManager.GetIcon(iconName);

        var card = new Border
        {
            Padding = new Thickness(12),
            BackgroundColor = WidgetCardBackground,
            Stroke = new SolidColorBrush(SettingsManager.SecondaryColor),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = widgetType.ToString(),
                        FontSize = 16,
                        TextColor = SettingsManager.FontColor,
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                    image
                }
            }
        };
        _widgetCards.Add((card, widgetType));

#if WINDOWS
        bool dragging = false;
        Microsoft.UI.Xaml.Input.Pointer? pointer = null;
        Microsoft.UI.Xaml.UIElement? nativeRoot = null;

        card.HandlerChanged += (_, _) =>
        {
            if (card.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement native) return;

            void FinalizeDrag(object s, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
            {
                dragging = false;
                if (s is Microsoft.UI.Xaml.UIElement el && pointer != null)
                    el.ReleasePointerCapture(pointer);
                pointer = null;
                var pt = e.GetCurrentPoint(nativeRoot).Position;
                WidgetCardEndDrag(new Point(pt.X, pt.Y));
            }

            native.PointerPressed += (s, e) =>
            {
                nativeRoot ??= _dragRoot.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
                dragging = true;
                pointer = e.Pointer;
                ((Microsoft.UI.Xaml.UIElement)s).CapturePointer(e.Pointer);
                var pt = e.GetCurrentPoint(nativeRoot).Position;
                WidgetCardBeginDrag(widgetType, new Point(pt.X, pt.Y));
            };

            native.PointerMoved += (s, e) =>
            {
                if (!dragging) return;
                var pt = e.GetCurrentPoint(nativeRoot).Position;
                WidgetCardMoveDrag(new Point(pt.X, pt.Y));
            };

            native.PointerReleased += (s, e) => { if (!dragging) return; FinalizeDrag(s, e); };
            native.PointerCaptureLost += (s, e) => { if (!dragging) return; FinalizeDrag(s, e); };
        };
#endif

        return card;
    }

       
    private void HighlightControllerFile()
    {
        foreach (var child in ControllerListStack.Children.OfType<VerticalStackLayout>())
        {
            var label = child.Children.OfType<Label>().FirstOrDefault();
            child.BackgroundColor = label?.Text == _selectedController?.Name
                ? FileHighlightBackground : Colors.Transparent;
        }
    }
    
    private void OnControllerFileClicked(Controller controller)
    {
        _selectedController = controller;
        HighlightControllerFile();
        LoadController(controller);
    }
    private void OnNewControllerFileClicked(object sender, EventArgs e) { }
    internal async Task DeleteWidgetAsync(string widgetId)
    {
        if (_selectedController == null) return;
        bool confirmed = await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Delete Widget", "Delete this widget?", "Delete", "Cancel");
        if (!confirmed) return;
        InspectorPanel.IsVisible = false;
        InspectorPanel.Content = null;
        _controllerView?.ClearLastSelectedWidget();
        _selectedController.RemoveWidget(widgetId);
        _controllerView?.Invalidate();
        await ControllerManager.SaveControllerAsync(_selectedController.Name);
    }

    private void WidgetCardBeginDrag(WidgetType widgetType, Point pos)
    {
        _dragWidgetType = widgetType;
        _dragPreview = _dragPreviews[widgetType];
        _dragPreview.Opacity = 0.85;
        DragOverlay.Add(_dragPreview);
        WidgetCardMoveDrag(pos);
    }
    private void WidgetCardMoveDrag(Point pos)
    {
        if (_dragPreview == null) return;
        double w = _dragPreview.Width > 0 ? _dragPreview.Width : 164;
        double h = _dragPreview.Height > 0 ? _dragPreview.Height : 196;
        AbsoluteLayout.SetLayoutBounds(_dragPreview, new Rect(pos.X - w / 2, pos.Y - h / 2, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(_dragPreview, AbsoluteLayoutFlags.None);
    }
    private async void WidgetCardEndDrag(Point pos)
    {
        if (_dragPreview != null)
        {
            _dragPreview.Opacity = 1.0;
            DragOverlay.Remove(_dragPreview);
            _dragPreview = null;
        }

        if (_selectedController == null || _scalingWrapper == null || _scale <= 0) return;

        var wrapperBounds = GetBoundsRelativeTo(_scalingWrapper, _dragRoot);
        if (!wrapperBounds.Contains(pos)) return;

        double dropX = (pos.X - wrapperBounds.X) / _scale;
        double dropY = (pos.Y - wrapperBounds.Y) / _scale;
        var widgetId = Guid.NewGuid().ToString("N")[..8];

        WidgetDescriptor descriptor = _dragWidgetType switch
        {
            WidgetType.Dial => new DialWidgetDescriptor     { WidgetId = widgetId, X = dropX - 202, Y = dropY - 202 },
            WidgetType.Joystick => new JoystickWidgetDescriptor { WidgetId = widgetId, X = dropX - 152, Y = dropY - 152 },
            _ => throw new ArgumentOutOfRangeException()
        };

        _selectedController.AddWidget(descriptor);
        _controllerView?.Invalidate();
        await ControllerManager.SaveControllerAsync(_selectedController.Name);
    }
    private static Rect GetBoundsRelativeTo(View target, View ancestor)
    {
        double x = 0, y = 0;
        Element? current = target;
        while (current != null && current != ancestor)
        {
            if (current is View v) { x += v.X; y += v.Y; }
            current = current.Parent;
        }
        return new Rect(x, y, target.Width, target.Height);
    }
}
