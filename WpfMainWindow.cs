using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;

namespace EternNotes
{
    public class MainWindow : Window
    {
        private Database db;
        private Project activeProject;
        private Point dragStartPoint;
        private bool isPanning = false;
        private bool isPanningPossible = false;
        private Point panningStartPoint;
        private double panningStartOffset;

        // WPF layout panels
        private Grid mainGrid;
        private Grid contentGrid;
        private Grid workspaceGrid;
        private StackPanel projectListPanel;
        private bool isSidebarCollapsed = false;
        private double sidebarPreviousWidth = 240;
        private Border btnCollapseSidebar;
        private Border btnExpandSidebar;
        private StackPanel projInfoPanel;
        private UIElement menuBarControl;
        private bool isMenuContextOpen = false;
        
        // Kanban layout
        private Grid kanbanGrid;

        // Workspace header
        private System.Windows.Shapes.Path projectIconPath;
        private TextBlock txtProjectName;
        private TextBlock txtProjectDesc;

        // Brushes
        private readonly Brush BgMain = new SolidColorBrush(Color.FromRgb(18, 18, 18));
        private readonly Brush BgSidebar = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        private readonly Brush BgCard = new SolidColorBrush(Color.FromRgb(33, 33, 33));
        private readonly Brush BgCardHover = new SolidColorBrush(Color.FromRgb(42, 42, 42));
        private readonly Brush BorderColor = new SolidColorBrush(Color.FromRgb(48, 48, 48));
        private readonly Brush TextActive = Brushes.White;
        private readonly Brush TextMuted = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        private readonly Brush AccentTodo = new SolidColorBrush(Color.FromRgb(150, 40, 40));
        private readonly Brush AccentOngoing = new SolidColorBrush(Color.FromRgb(20, 100, 85));
        private readonly Brush AccentCompleted = new SolidColorBrush(Color.FromRgb(90, 30, 120));

        [STAThread]
        public static void Main()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    System.IO.File.WriteAllText("crash_log.txt", e.ExceptionObject.ToString());
                };
                var app = new Application();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    System.IO.File.WriteAllText("crash_log.txt", e.Exception.ToString());
                };
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash_log.txt", ex.ToString());
            }
        }

        public MainWindow()
        {
            // Set Window parameters for modern borderless look
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18));
            Width = 1120;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            // Auto-collapse sidebar when window width is resized to small size
            this.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width < 780)
                {
                    CollapseSidebar();
                }
            };

            // Allow true full screen maximization covering the entire screen including taskbar
            MaxHeight = double.PositiveInfinity;
            MaxWidth = double.PositiveInfinity;

            // Style scrollbars to be thin, dark, matching VS Code aesthetic
            try
            {
                string scrollBarXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
                    <Setter Property='Width' Value='7'/>
                    <Setter Property='Height' Value='7'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ScrollBar'>
                                <Grid Background='#121212' SnapsToDevicePixels='true'>
                                    <Track Name='PART_Track' IsDirectionReversed='true'>
                                        <Track.Thumb>
                                            <Thumb>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='Thumb'>
                                                        <Border CornerRadius='3.5' Background='#3e3e3e' />
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(scrollBarXaml);
                Application.Current.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "ScrollBars: " + ex.ToString() + "\n");
            }

            // Style Slider to match dark premium theme
            try
            {
                string sliderXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>
                    <Setter Property='Background' Value='Transparent' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Slider'>
                                <Grid SnapsToDevicePixels='true'>
                                    <!-- Sleek rounded dark track -->
                                    <Border Name='TrackBackground' Background='#2d2d2d' BorderBrush='#444444' BorderThickness='1' CornerRadius='3' Height='6' VerticalAlignment='Center' Margin='5,0,5,0' />
                                    <Track Name='PART_Track'>
                                        <Track.DecreaseRepeatButton>
                                            <RepeatButton Command='Slider.DecreaseLarge'>
                                                <RepeatButton.Template>
                                                    <ControlTemplate TargetType='RepeatButton'>
                                                        <Border Background='Transparent' />
                                                    </ControlTemplate>
                                                </RepeatButton.Template>
                                            </RepeatButton>
                                        </Track.DecreaseRepeatButton>
                                        <Track.IncreaseRepeatButton>
                                            <RepeatButton Command='Slider.IncreaseLarge'>
                                                <RepeatButton.Template>
                                                    <ControlTemplate TargetType='RepeatButton'>
                                                        <Border Background='Transparent' />
                                                    </ControlTemplate>
                                                </RepeatButton.Template>
                                            </RepeatButton>
                                        </Track.IncreaseRepeatButton>
                                        <Track.Thumb>
                                            <!-- Custom circular thumb with gray visual states -->
                                            <Thumb Width='14' Height='14' Focusable='false'>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='Thumb'>
                                                        <Border Name='ThumbBorder' CornerRadius='7' Background='#888888' BorderBrush='#555555' BorderThickness='1' Cursor='Hand' />
                                                        <ControlTemplate.Triggers>
                                                            <Trigger Property='IsMouseOver' Value='true'>
                                                                <Setter TargetName='ThumbBorder' Property='Background' Value='#aaaaaa' />
                                                                <Setter TargetName='ThumbBorder' Property='BorderBrush' Value='#777777' />
                                                            </Trigger>
                                                        </ControlTemplate.Triggers>
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(sliderXaml);
                Application.Current.Resources[typeof(Slider)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "Slider: " + ex.ToString() + "\n");
            }

            // Style ComboBox to match dark premium theme
            try
            {
                string comboBoxXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ComboBox'>
                    <Setter Property='Background' Value='#333333' />
                    <Setter Property='Foreground' Value='White' />
                    <Setter Property='BorderBrush' Value='#555555' />
                    <Setter Property='BorderThickness' Value='1' />
                    <Setter Property='Padding' Value='6,4,6,4' />
                    <Setter Property='SnapsToDevicePixels' Value='true' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ComboBox'>
                                <Grid Name='MainGrid'>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width='*' />
                                        <ColumnDefinition Width='20' />
                                    </Grid.ColumnDefinitions>
                                    <ToggleButton Grid.ColumnSpan='2' Name='ToggleButton' Background='#333333' BorderBrush='#555555' BorderThickness='1' ClickMode='Press' Focusable='false' IsChecked='{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
                                        <ToggleButton.Template>
                                            <ControlTemplate TargetType='ToggleButton'>
                                                <Border Name='Border' Background='#333333' BorderBrush='#555555' BorderThickness='1' CornerRadius='4' />
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property='IsMouseOver' Value='true'>
                                                        <Setter TargetName='Border' Property='Background' Value='#444444' />
                                                        <Setter TargetName='Border' Property='BorderBrush' Value='#007acc' />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </ToggleButton.Template>
                                    </ToggleButton>
                                    <ContentPresenter Name='ContentSite' IsHitTestVisible='false' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}' Margin='8,4,20,4' VerticalAlignment='Center' HorizontalAlignment='Left' />
                                    <Path Grid.Column='1' Name='Arrow' Fill='#888888' HorizontalAlignment='Center' VerticalAlignment='Center' Data='M0,0 L3,3 L6,0 Z' IsHitTestVisible='false' />
                                    <Popup Name='Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}' AllowsTransparency='true' Focusable='false' PopupAnimation='Slide'>
                                        <Grid Name='DropDown' SnapsToDevicePixels='true' MinWidth='{TemplateBinding ActualWidth}' MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                                            <Border Name='DropDownBorder' Background='#1a1a1a' BorderBrush='#3c3c3c' BorderThickness='1' CornerRadius='4' Margin='0,2,0,0' />
                                            <ScrollViewer Margin='4' SnapsToDevicePixels='true'>
                                                <StackPanel IsItemsHost='true' KeyboardNavigation.DirectionalNavigation='Contained' />
                                            </ScrollViewer>
                                        </Grid>
                                    </Popup>
                                </Grid>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='HasItems' Value='false'>
                                        <Setter TargetName='DropDownBorder' Property='Height' Value='95' />
                                    </Trigger>
                                    <Trigger Property='IsMouseOver' Value='true'>
                                        <Setter Property='BorderBrush' Value='#007acc' />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(comboBoxXaml);
                Application.Current.Resources[typeof(ComboBox)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "ComboBox: " + ex.ToString() + "\n");
            }

            // Style ComboBoxItem to match dark premium theme dropdown items
            try
            {
                string comboBoxItemXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ComboBoxItem'>
                    <Setter Property='Background' Value='Transparent' />
                    <Setter Property='Foreground' Value='White' />
                    <Setter Property='Padding' Value='8,4,8,4' />
                    <Setter Property='SnapsToDevicePixels' Value='true' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ComboBoxItem'>
                                <Border Name='Border' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}' CornerRadius='3' Margin='1,1,1,1' SnapsToDevicePixels='true'>
                                    <ContentPresenter />
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsMouseOver' Value='true'>
                                        <Setter TargetName='Border' Property='Background' Value='#007acc' />
                                        <Setter Property='Foreground' Value='White' />
                                    </Trigger>
                                    <Trigger Property='IsSelected' Value='true'>
                                        <Setter TargetName='Border' Property='Background' Value='#3a3a3a' />
                                        <Setter TargetName='Border' Property='BorderBrush' Value='#007acc' />
                                        <Setter Property='Foreground' Value='White' />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(comboBoxItemXaml);
                Application.Current.Resources[typeof(ComboBoxItem)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "ComboBoxItem: " + ex.ToString() + "\n");
            }

            // Style MenuItem globally to remove default Windows icon column and white blobs
            try
            {
                string menuItemXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='MenuItem'>
                    <Setter Property='Foreground' Value='White' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='MenuItem'>
                                <Border Name='Border' Background='Transparent' Padding='12,6,24,6' SnapsToDevicePixels='true'>
                                    <ContentPresenter ContentSource='Header' RecognizesAccessKey='True' HorizontalAlignment='Left' VerticalAlignment='Center' />
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsHighlighted' Value='True'>
                                        <Setter TargetName='Border' Property='Background' Value='#007acc' />
                                        <Setter Property='Foreground' Value='White' />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(menuItemXaml);
                Application.Current.Resources[typeof(MenuItem)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "MenuItem: " + ex.ToString() + "\n");
            }

            // Style ContextMenu globally to match dark theme with rounded borders
            try
            {
                string contextMenuXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContextMenu'>
                    <Setter Property='Background' Value='#1a1a1a' />
                    <Setter Property='BorderBrush' Value='#333333' />
                    <Setter Property='BorderThickness' Value='1' />
                    <Setter Property='Padding' Value='0,4,0,4' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ContextMenu'>
                                <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='4' SnapsToDevicePixels='true'>
                                    <ItemsPresenter SnapsToDevicePixels='{TemplateBinding SnapsToDevicePixels}' />
                                </Border>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(contextMenuXaml);
                Application.Current.Resources[typeof(ContextMenu)] = style;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("style_errors.txt", "ContextMenu: " + ex.ToString() + "\n");
            }

            // Load data
            db = Storage.Load();
            if (db.Projects.Count > 0)
            {
                activeProject = db.Projects[0];
            }

            InitializeLayout();
            RefreshProjects();
            RefreshWorkspace();

            // Set up background DispatcherTimer to check deadlines
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(20);
            timer.Tick += (s, e) => CheckDeadlinesAndNotify();
            timer.Start();

            // Initial notification check
            CheckDeadlinesAndNotify();
        }

        private void InitializeLayout()
        {
            // Outer root border for rounded corners & shadow
            var rootBorder = new Border
            {
                Background = BgMain,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };
            this.Content = rootBorder;

            // Main Grid split: Title Bar, Menu Bar vs. Content
            mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // Title bar (Row 0)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) }); // Menu bar (Row 1)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content (Row 2)
            rootBorder.Child = mainGrid;

            // Setup WindowChrome for native border resizing and title bar dragging
            var chrome = new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 36,
                CornerRadius = new CornerRadius(8),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6)
            };
            System.Windows.Shell.WindowChrome.SetWindowChrome(this, chrome);

            // 1. Title Bar (Border container to prevent top-left and top-right corner bleed)
            var titleBar = new Border
            {
                Background = BgSidebar,
                CornerRadius = new CornerRadius(7, 7, 0, 0)
            };
            mainGrid.Children.Add(titleBar);
            Grid.SetRow(titleBar, 0);

            var titleBarGrid = new Grid();
            titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title & Logo
            titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Window controls (_ □ X)
            titleBar.Child = titleBarGrid;

            // Logo & Title
            var logoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(logoPanel, true);

            // Drag window on title bar click, toggle maximize on double click
            logoPanel.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            var logoIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Gamepad, new SolidColorBrush(Color.FromRgb(0, 122, 204)), 18);
            logoPanel.Children.Add(logoIcon);

            var titleText = new TextBlock
            {
                Text = "Etern-Notes // Native Workspace",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            logoPanel.Children.Add(titleText);
            titleBarGrid.Children.Add(logoPanel);
            Grid.SetColumn(logoPanel, 0);

            // Close & Minimize controls stack
            var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            titleBarGrid.Children.Add(controlsPanel);
            Grid.SetColumn(controlsPanel, 1);

            // Minimize button
            var btnMin = CreateTitleButton(WpfVectorIcons.Minimize, "Minimizar", (s, e) => WindowState = WindowState.Minimized);
            controlsPanel.Children.Add(btnMin);

            // Maximize / Restore button
            Button btnMax = null;
            btnMax = CreateTitleButton(WpfVectorIcons.Maximize, "Maximizar", (s, e) =>
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                    ((System.Windows.Shapes.Path)btnMax.Content).Data = Geometry.Parse(WpfVectorIcons.Maximize);
                    btnMax.ToolTip = "Maximizar";
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                    ((System.Windows.Shapes.Path)btnMax.Content).Data = Geometry.Parse(WpfVectorIcons.Restore);
                    btnMax.ToolTip = "Restaurar";
                }
            });
            controlsPanel.Children.Add(btnMax);

            // Sync maximize button icon when window state changes (e.g. double click on title bar)
            this.StateChanged += (s, e) =>
            {
                if (btnMax != null)
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        ((System.Windows.Shapes.Path)btnMax.Content).Data = Geometry.Parse(WpfVectorIcons.Restore);
                        btnMax.ToolTip = "Restaurar";
                    }
                    else
                    {
                        ((System.Windows.Shapes.Path)btnMax.Content).Data = Geometry.Parse(WpfVectorIcons.Maximize);
                        btnMax.ToolTip = "Maximizar";
                    }
                }
            };

            // Close button
            var btnClose = CreateTitleButton(WpfVectorIcons.Close, "Cerrar", (s, e) => { Storage.Save(db); this.Close(); }, isCloseButton: true);
            controlsPanel.Children.Add(btnClose);

            // 2. Menu Bar (Row 1 - Auto Show on Hover)
            menuBarControl = CreateMenuBarControl();
            menuBarControl.Visibility = Visibility.Collapsed;
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(menuBarControl, true);
            mainGrid.Children.Add(menuBarControl);
            Grid.SetRow(menuBarControl, 1);
            mainGrid.RowDefinitions[1].Height = new GridLength(0);

            // 3. Content Layout Grid (Row 2)
            contentGrid = new Grid();
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(contentGrid, true);
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240), MinWidth = 180, MaxWidth = 400 }); // Sidebar
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) }); // Splitter
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Workspace
            mainGrid.Children.Add(contentGrid);
            Grid.SetRow(contentGrid, 2);

            // Hover triggers to show/hide menu bar:
            // 1) Hovering over logoPanel (title bar) or menuBarControl keeps menu bar open
            logoPanel.MouseEnter += (s, e) => ShowTopMenuBar();
            menuBarControl.MouseEnter += (s, e) => ShowTopMenuBar();

            // 2) Hovering over window controls (_ □ X) or main workspace (contentGrid) closes menu bar
            controlsPanel.MouseEnter += (s, e) =>
            {
                if (!isMenuContextOpen) HideTopMenuBar();
            };

            contentGrid.MouseEnter += (s, e) =>
            {
                if (!isMenuContextOpen) HideTopMenuBar();
            };

            // Sidebar Column 0 (Border container to prevent bottom-left corner bleed)
            var sidebarGrid = new Border
            {
                Background = BgSidebar,
                CornerRadius = new CornerRadius(0, 0, 0, 7)
            };
            contentGrid.Children.Add(sidebarGrid);
            Grid.SetColumn(sidebarGrid, 0);

            // Sidebar border divider
            var sidebarBorder = new Border { BorderBrush = BorderColor, BorderThickness = new Thickness(0, 0, 1, 0) };
            sidebarGrid.Child = sidebarBorder;

            // Sidebar Inner Stack
            var sidebarStack = new Grid();
            sidebarStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) }); // Section Title
            sidebarStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Projects Scroll
            sidebarStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(90) }); // Footer Actions & Zoom
            sidebarBorder.Child = sidebarStack;

            // Section Title & Collapse Sidebar Button
            var sidebarHeader = new Grid { Margin = new Thickness(20, 15, 15, 0) };
            sidebarHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sidebarHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sidebarStack.Children.Add(sidebarHeader);
            Grid.SetRow(sidebarHeader, 0);

            var txtSecTitle = new TextBlock
            {
                Text = "PROYECTOS",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 110)),
                VerticalAlignment = VerticalAlignment.Center
            };
            sidebarHeader.Children.Add(txtSecTitle);
            Grid.SetColumn(txtSecTitle, 0);

            btnCollapseSidebar = new Border
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            var pathCollapse = WpfVectorIcons.GetIcon(WpfVectorIcons.SidebarToggle, TextMuted, 13);
            pathCollapse.HorizontalAlignment = HorizontalAlignment.Center;
            pathCollapse.VerticalAlignment = VerticalAlignment.Center;
            btnCollapseSidebar.Child = pathCollapse;

            btnCollapseSidebar.MouseEnter += (s, e) => { btnCollapseSidebar.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)); pathCollapse.Fill = TextActive; };
            btnCollapseSidebar.MouseLeave += (s, e) => { btnCollapseSidebar.Background = Brushes.Transparent; pathCollapse.Fill = TextMuted; };
            btnCollapseSidebar.MouseDown += (s, e) => { ToggleSidebar(); };

            sidebarHeader.Children.Add(btnCollapseSidebar);
            Grid.SetColumn(btnCollapseSidebar, 1);

            // Project list ScrollViewer
            var scrollProjects = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 5, 0, 5) };
            projectListPanel = new StackPanel();
            scrollProjects.Content = projectListPanel;
            sidebarStack.Children.Add(scrollProjects);
            Grid.SetRow(scrollProjects, 1);

            // Sidebar Footer Grid
            var sidebarFooter = new Grid { Margin = new Thickness(15, 0, 15, 15) };
            sidebarFooter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) }); // "+ Nuevo Proyecto"
            sidebarFooter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) }); // Zoom Slider
            sidebarStack.Children.Add(sidebarFooter);
            Grid.SetRow(sidebarFooter, 2);

            // "+ Nuevo Proyecto" button
            var btnAddProj = CreateFlatButton("+ Nuevo Proyecto", new SolidColorBrush(Color.FromRgb(45, 45, 48)), new SolidColorBrush(Color.FromRgb(55, 55, 60)), TextActive);
            btnAddProj.FontSize = 11;
            btnAddProj.FontWeight = FontWeights.Bold;
            btnAddProj.Click += (s, e) => ShowAddProjectDialog();
            sidebarFooter.Children.Add(btnAddProj);
            Grid.SetRow(btnAddProj, 0);

            // Zoom Controller Stack
            var zoomStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var lblZoom = new TextBlock { Text = "Escala", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Width = 40, VerticalAlignment = VerticalAlignment.Center };
            zoomStack.Children.Add(lblZoom);

            var zoomSlider = new Slider { Minimum = 0.4, Maximum = 1.2, Value = 1.0, Width = 110, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
            zoomStack.Children.Add(zoomSlider);

            var zoomValText = new TextBlock { Text = "100%", Foreground = TextMuted, FontSize = 10, Width = 35, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            zoomStack.Children.Add(zoomValText);
            sidebarFooter.Children.Add(zoomStack);
            Grid.SetRow(zoomStack, 1);

            // Layout Scale Transformation bound to Slider
            var scale = new ScaleTransform(1, 1);
            zoomSlider.ValueChanged += (s, e) =>
            {
                scale.ScaleX = zoomSlider.Value;
                scale.ScaleY = zoomSlider.Value;
                zoomValText.Text = Math.Round(zoomSlider.Value * 100) + "%";
            };

            // Grid Splitter Column 1
            var splitter = new GridSplitter
            {
                Width = 4,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            splitter.MouseEnter += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            splitter.MouseLeave += (s, e) => splitter.Background = Brushes.Transparent;
            contentGrid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);

            // Workspace Grid Column 2
            workspaceGrid = new Grid();
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Header
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Kanban
            contentGrid.Children.Add(workspaceGrid);
            Grid.SetColumn(workspaceGrid, 2);

            // 3. Workspace Header (split into columns: Toggle Button, Project Info, Actions)
            var header = new Grid { Background = BgMain };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 0: Toggle Sidebar
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Column 1: Info details
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 2: Action buttons

            var bottomBorder = new Border { BorderBrush = BorderColor, BorderThickness = new Thickness(0, 0, 0, 1) };
            Grid.SetColumnSpan(bottomBorder, 3);
            header.Children.Add(bottomBorder);
            workspaceGrid.Children.Add(header);
            Grid.SetRow(header, 0);

            // Toggle Sidebar Button (Expand Button, hidden by default when expanded)
            btnExpandSidebar = new Border
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            var pathToggle = WpfVectorIcons.GetIcon(WpfVectorIcons.SidebarToggle, TextMuted, 16);
            pathToggle.HorizontalAlignment = HorizontalAlignment.Center;
            pathToggle.VerticalAlignment = VerticalAlignment.Center;
            btnExpandSidebar.Child = pathToggle;

            btnExpandSidebar.MouseEnter += (s, e) => { btnExpandSidebar.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)); pathToggle.Fill = TextActive; };
            btnExpandSidebar.MouseLeave += (s, e) => { btnExpandSidebar.Background = Brushes.Transparent; pathToggle.Fill = TextMuted; };
            btnExpandSidebar.MouseDown += (s, e) => { ToggleSidebar(); };

            header.Children.Add(btnExpandSidebar);
            Grid.SetColumn(btnExpandSidebar, 0);

            // Project Info Details (Column 1)
            projInfoPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(35, 0, 0, 0) };
            projectIconPath = WpfVectorIcons.GetIcon(WpfVectorIcons.Folder, TextActive, 28);
            projInfoPanel.Children.Add(projectIconPath);

            var projTitles = new StackPanel { Margin = new Thickness(15, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            txtProjectName = new TextBlock { Text = "Proyecto", Foreground = TextActive, FontSize = 18, FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
            txtProjectDesc = new TextBlock { Text = "Descripción", Foreground = TextMuted, FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            projTitles.Children.Add(txtProjectName);
            projTitles.Children.Add(txtProjectDesc);
            projInfoPanel.Children.Add(projTitles);
            header.Children.Add(projInfoPanel);
            Grid.SetColumn(projInfoPanel, 1);

            // Header Actions Buttons Stack (Column 2)
            var headerActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 35, 0) };
            header.Children.Add(headerActions);
            Grid.SetColumn(headerActions, 2);

            var btnAddCol = CreateFlatButton("+ Añadir Columna", new SolidColorBrush(Color.FromRgb(40, 40, 40)), new SolidColorBrush(Color.FromRgb(55, 55, 60)), TextActive);
            btnAddCol.Padding = new Thickness(14, 6, 14, 6);
            btnAddCol.FontSize = 12;
            btnAddCol.FontWeight = FontWeights.Bold;
            btnAddCol.Margin = new Thickness(0, 0, 10, 0);
            btnAddCol.Click += (s, e) => ShowAddColumnDialog();
            headerActions.Children.Add(btnAddCol);

            var btnAddTask = CreateFlatButton("+ Añadir Tarea", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnAddTask.Padding = new Thickness(14, 6, 14, 6);
            btnAddTask.FontSize = 12;
            btnAddTask.FontWeight = FontWeights.Bold;
            btnAddTask.Click += (s, e) => ShowTaskDialog(null);
            headerActions.Children.Add(btnAddTask);

            // 4. ScrollViewer for Kanban columns (horizontal scrolling support)
            var scrollKanban = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(35, 20, 35, 20)
            };
            workspaceGrid.Children.Add(scrollKanban);
            Grid.SetRow(scrollKanban, 1);

            kanbanGrid = new Grid { LayoutTransform = scale };
            scrollKanban.Content = kanbanGrid;

            // Horizontal drag-to-scroll (panning) using right-click drag over columns/cards/canvas
            scrollKanban.PreviewMouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    isPanningPossible = true;
                    isPanning = false;
                    panningStartPoint = e.GetPosition(scrollKanban);
                    panningStartOffset = scrollKanban.HorizontalOffset;
                }
            };

            scrollKanban.PreviewMouseMove += (s, e) =>
            {
                if (isPanningPossible || isPanning)
                {
                    Point currentPoint = e.GetPosition(scrollKanban);
                    double deltaX = currentPoint.X - panningStartPoint.X;
                    double deltaY = currentPoint.Y - panningStartPoint.Y;

                    if (!isPanning)
                    {
                        // Check drag threshold (5 pixels)
                        if (Math.Abs(deltaX) > 5 || Math.Abs(deltaY) > 5)
                        {
                            isPanning = true;
                            scrollKanban.CaptureMouse();
                            scrollKanban.Cursor = Cursors.Hand;
                        }
                    }

                    if (isPanning)
                    {
                        scrollKanban.ScrollToHorizontalOffset(panningStartOffset - deltaX);
                        e.Handled = true; // Prevent event from triggering other actions (like selections or cards dragging)
                    }
                }
            };

            scrollKanban.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    if (isPanning)
                    {
                        scrollKanban.ReleaseMouseCapture();
                        scrollKanban.Cursor = Cursors.Arrow;
                        isPanning = false;
                        isPanningPossible = false;
                        e.Handled = true; // Suppress context menu trigger on drag end
                    }
                    else
                    {
                        isPanningPossible = false;
                    }
                }
            };

            scrollKanban.LostMouseCapture += (s, e) =>
            {
                scrollKanban.Cursor = Cursors.Arrow;
                isPanning = false;
                isPanningPossible = false;
            };

            // Direct MouseWheel over columns panel to zoom scale directly
            scrollKanban.PreviewMouseWheel += (s, e) =>
            {
                double step = 0.05;
                double newZoom = zoomSlider.Value + (e.Delta > 0 ? step : -step);
                if (newZoom < zoomSlider.Minimum) newZoom = zoomSlider.Minimum;
                if (newZoom > zoomSlider.Maximum) newZoom = zoomSlider.Maximum;
                
                zoomSlider.Value = newZoom;
                e.Handled = true; // Intercept event completely to perform layout zoom
            };
        }

        private void ShowTopMenuBar()
        {
            if (menuBarControl != null)
            {
                menuBarControl.Visibility = Visibility.Visible;
                if (mainGrid != null && mainGrid.RowDefinitions.Count > 1)
                {
                    mainGrid.RowDefinitions[1].Height = new GridLength(34);
                }
            }
        }

        private void HideTopMenuBar()
        {
            if (menuBarControl != null && !isMenuContextOpen)
            {
                menuBarControl.Visibility = Visibility.Collapsed;
                if (mainGrid != null && mainGrid.RowDefinitions.Count > 1)
                {
                    mainGrid.RowDefinitions[1].Height = new GridLength(0);
                }
            }
        }

        private void CollapseSidebar()
        {
            if (!isSidebarCollapsed)
            {
                ToggleSidebar();
            }
        }

        private void ExpandSidebar()
        {
            if (isSidebarCollapsed)
            {
                ToggleSidebar();
            }
        }

        private void ToggleSidebar()
        {
            if (isSidebarCollapsed)
            {
                // Expand
                contentGrid.ColumnDefinitions[0].MinWidth = 180;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(sidebarPreviousWidth);
                contentGrid.ColumnDefinitions[1].Width = new GridLength(4);
                btnExpandSidebar.Visibility = Visibility.Collapsed;
                btnCollapseSidebar.Visibility = Visibility.Visible;
                projInfoPanel.Margin = new Thickness(35, 0, 0, 0);
                isSidebarCollapsed = false;
            }
            else
            {
                // Collapse
                sidebarPreviousWidth = contentGrid.ColumnDefinitions[0].ActualWidth;
                if (sidebarPreviousWidth < 50) sidebarPreviousWidth = 240;
                contentGrid.ColumnDefinitions[0].MinWidth = 0;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(0);
                contentGrid.ColumnDefinitions[1].Width = new GridLength(0);
                btnExpandSidebar.Visibility = Visibility.Visible;
                btnCollapseSidebar.Visibility = Visibility.Collapsed;
                projInfoPanel.Margin = new Thickness(12, 0, 0, 0);
                isSidebarCollapsed = true;
            }
        }

        private Border CreateKanbanColumn(string title, Brush accentBrush, out TextBlock countText, out StackPanel cardsPanel, string status, KanbanColumn columnObj)
        {
            var border = new Border
            {
                Background = BgMain,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                AllowDrop = true
            };

            var dock = new DockPanel();
            border.Child = dock;

            // Column Header (CornerRadius matching parent border to prevent top corner bleed)
            var colHeader = new Border
            {
                Background = accentBrush,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Height = 36
            };
            
            // Drag and Drop Event listeners for Column reordering
            colHeader.PreviewMouseLeftButtonDown += (s, e) =>
            {
                dragStartPoint = e.GetPosition(null);
            };

            colHeader.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var pos = e.GetPosition(null);
                    var diff = dragStartPoint - pos;
                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        // Ensure we are not clicking context menu or inputs
                        if (!(e.OriginalSource is TextBox || e.OriginalSource is ComboBox || e.OriginalSource is Button))
                        {
                            var data = new DataObject();
                            data.SetData("ColumnId", columnObj.Id);
                            DragDrop.DoDragDrop(colHeader, data, DragDropEffects.Move);
                        }
                    }
                }
            };
            
            // Context menu for column header (edit/duplicate/delete)
            var ctxMenu = new ContextMenu();

            var mnuEdit = new MenuItem { Header = "Editar Columna" };
            mnuEdit.Click += (s, e) => ShowEditColumnDialog(columnObj);
            ctxMenu.Items.Add(mnuEdit);

            var mnuDuplicateCol = new MenuItem { Header = "Duplicar Columna" };
            mnuDuplicateCol.Click += (s, e) => DuplicateColumn(columnObj);
            ctxMenu.Items.Add(mnuDuplicateCol);

            var mnuDelete = new MenuItem { Header = "Eliminar Columna" };
            mnuDelete.Click += (s, e) => DeleteColumn(columnObj);
            ctxMenu.Items.Add(mnuDelete);

            colHeader.ContextMenu = ctxMenu;

            DockPanel.SetDock(colHeader, Dock.Top);
            dock.Children.Add(colHeader);

            var headerGrid = new Grid { Margin = new Thickness(12, 0, 12, 0) };
            colHeader.Child = headerGrid;

            var txtTitle = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = TextActive,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerGrid.Children.Add(txtTitle);

            countText = new TextBlock
            {
                Text = "0",
                FontFamily = new FontFamily("JetBrains Mono"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = TextMuted,
                Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            // Border wrapper for rounding count background
            var countBorder = new Border { CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = countText, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            headerGrid.Children.Add(countBorder);

            // Cards scroll list
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(5) };
            dock.Children.Add(scroll);

            cardsPanel = new StackPanel { Margin = new Thickness(5, 5, 5, 20) };
            scroll.Content = cardsPanel;

            // Setup column drop logic
            border.DragOver += (s, e) =>
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            };

            border.Drop += (s, e) =>
            {
                string taskId = e.Data.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(taskId))
                {
                    var task = db.Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null && task.Status != status)
                    {
                        task.Status = status;
                        Storage.Save(db);
                        RefreshWorkspace();
                    }
                }
                else
                {
                    string dragColId = e.Data.GetData("ColumnId") as string;
                    if (!string.IsNullOrEmpty(dragColId) && dragColId != columnObj.Id)
                    {
                        int dragIdx = activeProject.Columns.FindIndex(c => c.Id == dragColId);
                        int targetIdx = activeProject.Columns.FindIndex(c => c.Id == columnObj.Id);
                        if (dragIdx >= 0 && targetIdx >= 0)
                        {
                            var dragCol = activeProject.Columns[dragIdx];
                            activeProject.Columns.RemoveAt(dragIdx);
                            activeProject.Columns.Insert(targetIdx, dragCol);
                            Storage.Save(db);
                            RefreshWorkspace();
                        }
                    }
                }
            };

            return border;
        }

        private void RefreshProjects()
        {
            projectListPanel.Children.Clear();

            foreach (var proj in db.Projects)
            {
                bool isSelected = activeProject != null && proj.Id == activeProject.Id;

                var btnBorder = new Border
                {
                    Background = isSelected ? new SolidColorBrush(Color.FromRgb(45, 45, 48)) : Brushes.Transparent,
                    BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) : Brushes.Transparent,
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Margin = new Thickness(0, 2, 0, 2),
                    Height = 34,
                    Cursor = Cursors.Hand
                };

                var itemGrid = new Grid { Margin = new Thickness(15, 0, 15, 0) };
                btnBorder.Child = itemGrid;

                // Select Icon representation
                string geometryStr = WpfVectorIcons.Folder;
                if (proj.IconType == "Gamepad") geometryStr = WpfVectorIcons.Gamepad;
                else if (proj.IconType == "Video") geometryStr = WpfVectorIcons.Video;

                var icon = WpfVectorIcons.GetIcon(geometryStr, isSelected ? TextActive : TextMuted, 14);
                icon.HorizontalAlignment = HorizontalAlignment.Left;
                icon.VerticalAlignment = VerticalAlignment.Center;
                itemGrid.Children.Add(icon);

                var txtName = new TextBlock
                {
                    Text = proj.Name,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12.5,
                    Foreground = isSelected ? TextActive : TextMuted,
                    Margin = new Thickness(22, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                itemGrid.Children.Add(txtName);

                // Hover states
                var localProj = proj;
                btnBorder.MouseEnter += (s, e) =>
                {
                    if (activeProject == null || localProj.Id != activeProject.Id)
                        btnBorder.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                };
                btnBorder.MouseLeave += (s, e) =>
                {
                    if (activeProject == null || localProj.Id != activeProject.Id)
                        btnBorder.Background = Brushes.Transparent;
                };

                // Context menu for project item (right click edit/duplicate/delete)
                var ctxMenu = new ContextMenu();

                var mnuEdit = new MenuItem { Header = "Editar Proyecto" };
                mnuEdit.Click += (s, e) => ShowEditProjectDialog(localProj);
                ctxMenu.Items.Add(mnuEdit);

                var mnuDuplicate = new MenuItem { Header = "Duplicar Proyecto" };
                mnuDuplicate.Click += (s, e) => DuplicateProject(localProj);
                ctxMenu.Items.Add(mnuDuplicate);

                var mnuDelete = new MenuItem { Header = "Eliminar Proyecto" };
                mnuDelete.Click += (s, e) => DeleteProject(localProj);
                ctxMenu.Items.Add(mnuDelete);

                btnBorder.ContextMenu = ctxMenu;

                // Click select
                btnBorder.MouseDown += (s, e) =>
                {
                    activeProject = localProj;
                    RefreshProjects();
                    RefreshWorkspace();
                };

                projectListPanel.Children.Add(btnBorder);
            }
        }

        private void RefreshWorkspace()
        {
            kanbanGrid.Children.Clear();
            kanbanGrid.ColumnDefinitions.Clear();

            if (activeProject == null)
            {
                txtProjectName.Text = "No hay proyecto seleccionado";
                txtProjectDesc.Text = "Crea un proyecto para empezar.";
                return;
            }

            // Bind icon path
            string iconGeometry = WpfVectorIcons.Folder;
            if (activeProject.IconType == "Gamepad") iconGeometry = WpfVectorIcons.Gamepad;
            else if (activeProject.IconType == "Video") iconGeometry = WpfVectorIcons.Video;
            projectIconPath.Data = Geometry.Parse(iconGeometry);

            txtProjectName.Text = activeProject.Name;
            txtProjectDesc.Text = activeProject.Description;

            int colCount = activeProject.Columns.Count;
            
            // Set up column definitions: 1 for column, 1 for spacing
            for (int i = 0; i < colCount; i++)
            {
                kanbanGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
                if (i < colCount - 1)
                {
                    kanbanGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                }
            }

            var columnPanels = new Dictionary<string, StackPanel>();
            var columnCountTexts = new Dictionary<string, TextBlock>();

            // Create columns dynamically
            for (int i = 0; i < colCount; i++)
            {
                var col = activeProject.Columns[i];
                var accentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(col.ColorHex));
                
                TextBlock txtCount;
                StackPanel cardPanel;
                
                var colBorder = CreateKanbanColumn(col.Name, accentBrush, out txtCount, out cardPanel, col.Id, col);
                
                columnPanels[col.Id] = cardPanel;
                columnCountTexts[col.Id] = txtCount;

                Grid.SetColumn(colBorder, i * 2);
                kanbanGrid.Children.Add(colBorder);
            }

            // Render tasks
            var columnTaskCounts = new Dictionary<string, int>();
            foreach (var col in activeProject.Columns)
            {
                columnTaskCounts[col.Id] = 0;
            }

            foreach (var task in db.Tasks.Where(t => t.ProjectId == activeProject.Id))
            {
                var card = CreateTaskCard(task);
                StackPanel panel;
                if (columnPanels.TryGetValue(task.Status, out panel))
                {
                    panel.Children.Add(card);
                    columnTaskCounts[task.Status]++;
                }
                else
                {
                    // Fallback to first column if status is invalid
                    if (activeProject.Columns.Count > 0)
                    {
                        var firstCol = activeProject.Columns[0];
                        task.Status = firstCol.Id;
                        columnPanels[firstCol.Id].Children.Add(card);
                        columnTaskCounts[firstCol.Id]++;
                    }
                }
            }

            // Update column counts
            foreach (var col in activeProject.Columns)
            {
                TextBlock txtCount;
                if (columnCountTexts.TryGetValue(col.Id, out txtCount))
                {
                    txtCount.Text = columnTaskCounts[col.Id].ToString();
                }
            }
        }

        private FrameworkElement CreateTaskCard(DeveloperTask task)
        {
            // Root Card border
            var cardBorder = new Border
            {
                Background = BgCard,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 4, 0, 6),
                Padding = new Thickness(12),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 1.5,
                    Opacity = 0.25,
                    BlurRadius = 3
                }
            };

            // Custom hover glows
            cardBorder.MouseEnter += (s, e) =>
            {
                cardBorder.Background = BgCardHover;
                cardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 75, 75));
            };
            cardBorder.MouseLeave += (s, e) =>
            {
                cardBorder.Background = BgCard;
                cardBorder.BorderBrush = BorderColor;
            };

            var layout = new StackPanel();
            cardBorder.Child = layout;

            // Tags row (WrapPanel)
            var tagsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            layout.Children.Add(tagsWrap);

            if (!string.IsNullOrEmpty(task.Tags))
            {
                foreach (var tag in task.Tags.Split(',').Select(t => t.Trim()))
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    var tagBorder = new Border
                    {
                        CornerRadius = new CornerRadius(3.5),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 5, 3),
                        BorderThickness = new Thickness(1)
                    };

                    var tagText = new TextBlock
                    {
                        Text = tag.ToUpper(),
                        FontFamily = new FontFamily("JetBrains Mono"),
                        FontSize = 8,
                        FontWeight = FontWeights.Bold
                    };
                    tagBorder.Child = tagText;

                    // Tag platform binding colors
                    string tagLower = tag.ToLower();
                    if (tagLower == "ios")
                    {
                        tagBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                        tagBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                        tagText.Foreground = Brushes.White;
                    }
                    else if (tagLower == "web")
                    {
                        tagBorder.Background = new SolidColorBrush(Color.FromRgb(13, 40, 71));
                        tagBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 139, 253));
                        tagText.Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255));
                    }
                    else if (tagLower == "desktop")
                    {
                        tagBorder.Background = new SolidColorBrush(Color.FromRgb(59, 34, 13));
                        tagBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 136, 62));
                        tagText.Foreground = new SolidColorBrush(Color.FromRgb(255, 158, 88));
                    }
                    else if (tagLower == "android")
                    {
                        tagBorder.Background = new SolidColorBrush(Color.FromRgb(11, 46, 22));
                        tagBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(57, 211, 83));
                        tagText.Foreground = new SolidColorBrush(Color.FromRgb(86, 236, 114));
                    }
                    else
                    {
                        tagBorder.Background = new SolidColorBrush(Color.FromRgb(33, 38, 45));
                        tagBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 79, 88));
                        tagText.Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217));
                    }

                    tagsWrap.Children.Add(tagBorder);
                }
            }

            // Task title
            var txtTitle = new TextBlock
            {
                Text = task.Title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = TextActive,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            layout.Children.Add(txtTitle);

            // Task description
            if (!string.IsNullOrEmpty(task.Description))
            {
                var txtDesc = new TextBlock
                {
                    Text = task.Description,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    Foreground = TextMuted,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                layout.Children.Add(txtDesc);
            }

            // Subtasks layout block (custom stylized checkboxes)
            if (task.SubTasks.Count > 0)
            {
                var subtasksBorder = new Border
                {
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                    Padding = new Thickness(0, 5, 0, 5),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var subPanel = new StackPanel();
                subtasksBorder.Child = subPanel;
                layout.Children.Add(subtasksBorder);

                for (int i = 0; i < task.SubTasks.Count; i++)
                {
                    var sub = task.SubTasks[i];
                    
                    var subRow = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 3, 0, 3),
                        Cursor = Cursors.Hand
                    };

                    var chkBox = new Border
                    {
                        Width = 13,
                        Height = 13,
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        CornerRadius = new CornerRadius(2.5),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    };

                    var checkSymbol = WpfVectorIcons.GetIcon(WpfVectorIcons.Check, Brushes.White, 7);
                    checkSymbol.VerticalAlignment = VerticalAlignment.Center;
                    checkSymbol.HorizontalAlignment = HorizontalAlignment.Center;
                    checkSymbol.Visibility = sub.Completed ? Visibility.Visible : Visibility.Collapsed;
                    chkBox.Child = checkSymbol;

                    if (sub.Completed)
                    {
                        chkBox.Background = new SolidColorBrush(Color.FromRgb(35, 134, 54));
                        chkBox.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 160, 67));
                    }

                    var chkText = new TextBlock
                    {
                        Text = sub.Title,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 11,
                        Foreground = sub.Completed ? TextMuted : TextActive,
                        TextDecorations = sub.Completed ? TextDecorations.Strikethrough : null,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    subRow.MouseDown += (s, e) =>
                    {
                        sub.Completed = !sub.Completed;
                        checkSymbol.Visibility = sub.Completed ? Visibility.Visible : Visibility.Collapsed;
                        chkBox.Background = sub.Completed ? new SolidColorBrush(Color.FromRgb(35, 134, 54)) : new SolidColorBrush(Color.FromRgb(30, 30, 30));
                        chkBox.BorderBrush = sub.Completed ? new SolidColorBrush(Color.FromRgb(46, 160, 67)) : new SolidColorBrush(Color.FromRgb(85, 85, 85));
                        chkText.Foreground = sub.Completed ? TextMuted : TextActive;
                        chkText.TextDecorations = sub.Completed ? TextDecorations.Strikethrough : null;
                        Storage.Save(db);
                    };

                    subRow.Children.Add(chkBox);
                    subRow.Children.Add(chkText);
                    subPanel.Children.Add(subRow);
                }
            }

            // Inline subtask input textbox
            var subtaskInputGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            var txtSubInput = new TextBox
            {
                Text = "+ Añadir subtarea...",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = TextMuted,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 2, 6, 2),
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI"),
                CaretBrush = Brushes.White,
                SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204))
            };
            
            txtSubInput.GotFocus += (s, e) =>
            {
                if (txtSubInput.Text == "+ Añadir subtarea...")
                {
                    txtSubInput.Text = "";
                    txtSubInput.Foreground = TextActive;
                }
            };
            
            txtSubInput.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtSubInput.Text.Trim()))
                {
                    txtSubInput.Text = "+ Añadir subtarea...";
                    txtSubInput.Foreground = TextMuted;
                }
            };

            txtSubInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    string text = txtSubInput.Text.Trim();
                    if (!string.IsNullOrEmpty(text) && text != "+ Añadir subtarea...")
                    {
                        task.SubTasks.Add(new SubTask { Title = text, Completed = false });
                        Storage.Save(db);
                        RefreshWorkspace();
                    }
                }
            };
            subtaskInputGrid.Children.Add(txtSubInput);
            layout.Children.Add(subtaskInputGrid);

            // Document link block
            if (!string.IsNullOrEmpty(task.Link))
            {
                var linkPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand };
                var linkIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Link, TextMuted, 11);
                var linkText = new TextBlock
                {
                    Text = "Document Link →",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10.5,
                    Foreground = TextMuted,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                
                linkPanel.Children.Add(linkIcon);
                linkPanel.Children.Add(linkText);

                linkPanel.MouseEnter += (s, e) => { linkText.Foreground = TextActive; linkIcon.Fill = TextActive; };
                linkPanel.MouseLeave += (s, e) => { linkText.Foreground = TextMuted; linkIcon.Fill = TextMuted; };

                linkPanel.MouseDown += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(task.Link) { UseShellExecute = true });
                    }
                    catch
                    {
                        ShowCustomMessageBox("No se pudo abrir el enlace: " + task.Link, "Error enlace", MessageBoxButton.OK);
                    }
                };

                layout.Children.Add(linkPanel);
            }

            // Card Footer Grid (assignee left, actions stack right)
            var cardFooter = new Grid();
            cardFooter.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            layout.Children.Add(cardFooter);

            // Assignee Column 0
            if (!string.IsNullOrEmpty(task.Assignee))
            {
                var assigneeStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                var userIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.User, new SolidColorBrush(Color.FromRgb(227, 179, 65)), 11);
                var txtAssignee = new TextBlock
                {
                    Text = task.Assignee,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(227, 179, 65)),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                assigneeStack.Children.Add(userIcon);
                assigneeStack.Children.Add(txtAssignee);
                cardFooter.Children.Add(assigneeStack);
                Grid.SetColumn(assigneeStack, 0);
            }

            // Actions Stack Column 1 (Deadline + Edit + Delete)
            var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            
            if (task.Deadline != DateTime.MinValue)
            {
                var calIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Calendar, TextMuted, 10.5);
                var txtDeadline = new TextBlock
                {
                    Text = task.Deadline.ToString("dd MMM"),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 9.5,
                    Foreground = TextMuted,
                    Margin = new Thickness(4, 0, 8, 0)
                };

                // Set deadline alerts
                if (task.Status != "Done")
                {
                    if (task.Deadline < DateTime.Now)
                    {
                        txtDeadline.Text = "Vencida";
                        txtDeadline.Foreground = new SolidColorBrush(Color.FromRgb(248, 81, 73));
                        calIcon.Fill = new SolidColorBrush(Color.FromRgb(248, 81, 73));
                    }
                    else if ((task.Deadline - DateTime.Now).TotalHours <= 24)
                    {
                        txtDeadline.Foreground = new SolidColorBrush(Color.FromRgb(227, 179, 65));
                        calIcon.Fill = new SolidColorBrush(Color.FromRgb(227, 179, 65));
                    }
                }

                actionsStack.Children.Add(calIcon);
                actionsStack.Children.Add(txtDeadline);
            }

            // Edit button action
            var btnEdit = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(4),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0),
                CornerRadius = new CornerRadius(3)
            };
            btnEdit.Child = WpfVectorIcons.GetIcon(WpfVectorIcons.Edit, TextMuted, 13);
            btnEdit.MouseEnter += (s, e) => { btnEdit.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)); ((System.Windows.Shapes.Path)btnEdit.Child).Fill = TextActive; };
            btnEdit.MouseLeave += (s, e) => { btnEdit.Background = Brushes.Transparent; ((System.Windows.Shapes.Path)btnEdit.Child).Fill = TextMuted; };
            btnEdit.MouseDown += (s, e) => { e.Handled = true; ShowTaskDialog(task); };
            actionsStack.Children.Add(btnEdit);

            // Delete button action
            var btnDel = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(4),
                Cursor = Cursors.Hand,
                CornerRadius = new CornerRadius(3)
            };
            btnDel.Child = WpfVectorIcons.GetIcon(WpfVectorIcons.Trash, TextMuted, 13);
            btnDel.MouseEnter += (s, e) => { btnDel.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)); ((System.Windows.Shapes.Path)btnDel.Child).Fill = new SolidColorBrush(Color.FromRgb(248, 81, 73)); };
            btnDel.MouseLeave += (s, e) => { btnDel.Background = Brushes.Transparent; ((System.Windows.Shapes.Path)btnDel.Child).Fill = TextMuted; };
            btnDel.MouseDown += (s, e) =>
            {
                e.Handled = true;
                if (ShowCustomMessageBox("¿Seguro que deseas eliminar esta tarea?", "Eliminar Tarea", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    db.Tasks.Remove(task);
                    Storage.Save(db);
                    RefreshWorkspace();
                }
            };
            actionsStack.Children.Add(btnDel);

            cardFooter.Children.Add(actionsStack);
            Grid.SetColumn(actionsStack, 1);

            // Drag and Drop Event listeners
            cardBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                dragStartPoint = e.GetPosition(null);
            };

            cardBorder.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var pos = e.GetPosition(null);
                    var diff = dragStartPoint - pos;
                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        // Check that mouse is not over inputs or hyperlinks
                        if (!(e.OriginalSource is TextBox || e.OriginalSource is Border && ((Border)e.OriginalSource).Cursor == Cursors.Hand))
                        {
                            var data = new DataObject(typeof(string), task.Id);
                            DragDrop.DoDragDrop(cardBorder, data, DragDropEffects.Move);
                        }
                    }
                }
            };

            // Click to edit
            cardBorder.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ShowTaskDialog(task);
                }
            };

            return cardBorder;
        }

        private void CheckDeadlinesAndNotify()
        {
            bool updates = false;
            foreach (var task in db.Tasks)
            {
                // Check if task is completed (status matches last column of its project)
                var taskProj = db.Projects.FirstOrDefault(p => p.Id == task.ProjectId);
                bool isCompleted = false;
                if (taskProj != null && taskProj.Columns.Count > 0)
                {
                    isCompleted = (task.Status == taskProj.Columns[taskProj.Columns.Count - 1].Id);
                }
                else
                {
                    isCompleted = (task.Status == "Done");
                }

                if (isCompleted || task.Notified || task.Deadline == DateTime.MinValue) continue;

                var diff = task.Deadline - DateTime.Now;
                if (diff.TotalHours > 0 && diff.TotalHours <= 6)
                {
                    // Dispatch sliding Toast on the UI thread
                    this.Dispatcher.Invoke(() =>
                    {
                        var toast = new ToastWindow(task, (t) =>
                        {
                            var proj = db.Projects.FirstOrDefault(p => p.Id == t.ProjectId);
                            if (proj != null && proj.Columns.Count > 0)
                            {
                                t.Status = proj.Columns[proj.Columns.Count - 1].Id;
                            }
                            else
                            {
                                t.Status = "Done";
                            }
                            Storage.Save(db);
                            RefreshWorkspace();
                        });
                        toast.Show();
                    });

                    task.Notified = true;
                    updates = true;
                }
            }

            if (updates)
            {
                Storage.Save(db);
            }
        }

        private void ShowEditProjectDialog(Project proj)
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            border.Child = grid;

            var headerText = new TextBlock { Text = "Editar Proyecto", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextActive, Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(headerText);
            Grid.SetRow(headerText, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = "Nombre del Proyecto", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtName = new TextBox { Text = proj.Name, Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtName);

            formStack.Children.Add(new TextBlock { Text = "Descripción", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtDesc = new TextBox { Text = proj.Description, Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtDesc);

            formStack.Children.Add(new TextBlock { Text = "Tipo de Icono", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var cbIcon = new ComboBox { FontSize = 11 };
            cbIcon.Items.Add("Folder");
            cbIcon.Items.Add("Gamepad");
            cbIcon.Items.Add("Video");
            cbIcon.SelectedItem = proj.IconType;
            formStack.Children.Add(cbIcon);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 80;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Guardar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnSave.Width = 80;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    ShowCustomMessageBox("Introduce el nombre del proyecto.", "Error", MessageBoxButton.OK);
                    return;
                }
                
                proj.Name = txtName.Text.Trim();
                proj.Description = txtDesc.Text.Trim();
                proj.IconType = cbIcon.SelectedItem.ToString();

                Storage.Save(db);
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                RefreshProjects();
                RefreshWorkspace();
            }
        }

        private void DeleteProject(Project targetProj)
        {
            if (targetProj == null) return;

            var result = ShowCustomMessageBox(
                "¿Estás seguro de que deseas eliminar el proyecto \"" + targetProj.Name + "\" y todas sus tareas asociadas?",
                "Eliminar Proyecto",
                MessageBoxButton.YesNo
            );

            if (result == MessageBoxResult.Yes)
            {
                db.Tasks.RemoveAll(t => t.ProjectId == targetProj.Id);
                db.Projects.Remove(targetProj);
                activeProject = db.Projects.Count > 0 ? db.Projects[0] : null;

                Storage.Save(db);
                RefreshProjects();
                RefreshWorkspace();
            }
        }

        private void DuplicateProject(Project originalProj)
        {
            if (originalProj == null) return;

            var newProj = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = originalProj.Name + " (Copia)",
                Description = originalProj.Description,
                IconType = originalProj.IconType
            };

            // Map old column IDs to new column IDs
            var colIdMap = new Dictionary<string, string>();
            foreach (var col in originalProj.Columns)
            {
                var newColId = Guid.NewGuid().ToString();
                colIdMap[col.Id] = newColId;
                newProj.Columns.Add(new KanbanColumn
                {
                    Id = newColId,
                    Name = col.Name,
                    ColorHex = col.ColorHex
                });
            }

            db.Projects.Add(newProj);

            // Copy all tasks
            var originalTasks = db.Tasks.Where(t => t.ProjectId == originalProj.Id).ToList();
            foreach (var task in originalTasks)
            {
                string newStatus = "ToDo";
                string mappedId;
                if (!string.IsNullOrEmpty(task.Status) && colIdMap.TryGetValue(task.Status, out mappedId))
                {
                    newStatus = mappedId;
                }
                else if (newProj.Columns.Count > 0)
                {
                    newStatus = newProj.Columns[0].Id;
                }

                // Copy subtasks
                var newSubTasks = new List<SubTask>();
                if (task.SubTasks != null)
                {
                    foreach (var sub in task.SubTasks)
                    {
                        newSubTasks.Add(new SubTask
                        {
                            Title = sub.Title,
                            Completed = sub.Completed
                        });
                    }
                }

                var newTask = new DeveloperTask
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = newProj.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    Tags = task.Tags,
                    Assignee = task.Assignee,
                    Link = task.Link,
                    Deadline = task.Deadline,
                    Status = newStatus,
                    Notified = false,
                    SubTasks = newSubTasks
                };
                db.Tasks.Add(newTask);
            }

            activeProject = newProj;
            Storage.Save(db);
            RefreshProjects();
            RefreshWorkspace();
        }

        private void ShowAddProjectDialog()
        {
            // Custom modern borderless project creation window
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Form
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons
            border.Child = grid;

            var headerText = new TextBlock { Text = "Nuevo Proyecto", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextActive, Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(headerText);
            Grid.SetRow(headerText, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = "Nombre del Proyecto", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtName = new TextBox { Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtName);

            formStack.Children.Add(new TextBlock { Text = "Descripción", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtDesc = new TextBox { Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtDesc);

            formStack.Children.Add(new TextBlock { Text = "Tipo de Icono", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var cbIcon = new ComboBox { FontSize = 11 };
            cbIcon.Items.Add("Folder");
            cbIcon.Items.Add("Gamepad");
            cbIcon.Items.Add("Video");
            cbIcon.SelectedIndex = 0;

            formStack.Children.Add(cbIcon);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 80;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Guardar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnSave.Width = 80;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    ShowCustomMessageBox("Introduce el nombre del proyecto.", "Error", MessageBoxButton.OK);
                    return;
                }
                
                var newProj = new Project
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = txtName.Text.Trim(),
                    Description = txtDesc.Text.Trim(),
                    IconType = cbIcon.SelectedItem.ToString()
                };
                newProj.Columns.Add(new KanbanColumn { Id = "ToDo", Name = "PLANIFICADO", ColorHex = "#962828" });
                newProj.Columns.Add(new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" });
                newProj.Columns.Add(new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5a1e78" });

                db.Projects.Add(newProj);
                activeProject = newProj;
                Storage.Save(db);
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                RefreshProjects();
                RefreshWorkspace();
            }
        }

        private ComboBoxItem CreateColorItem(string displayName, string hexColor)
        {
            var item = new ComboBoxItem();
            
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var circle = new Border
            {
                Width = 12,
                Height = 12,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)),
                CornerRadius = new CornerRadius(6),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            panel.Children.Add(circle);
            
            var label = new TextBlock
            {
                Text = displayName,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(label);
            
            item.Content = panel;
            item.Tag = hexColor;
            
            return item;
        }

        private void ShowEditColumnDialog(KanbanColumn col)
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 360,
                Height = 265,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            border.Child = grid;

            var titleText = new TextBlock { Text = "Editar Columna", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextActive, Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(titleText);
            Grid.SetRow(titleText, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = "Nombre de la Columna", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtName = new TextBox { Text = col.Name, Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtName);

            formStack.Children.Add(new TextBlock { Text = "Color de la Columna", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var cbColor = new ComboBox { FontSize = 11 };
            cbColor.Items.Add(CreateColorItem("Coral Red", "#962828"));
            cbColor.Items.Add(CreateColorItem("Emerald Green", "#1b6b3c"));
            cbColor.Items.Add(CreateColorItem("Ocean Blue", "#105b9e"));
            cbColor.Items.Add(CreateColorItem("Dark Teal", "#146455"));
            cbColor.Items.Add(CreateColorItem("Dark Purple", "#5a1e78"));
            cbColor.Items.Add(CreateColorItem("Sunset Orange", "#ac5616"));
            cbColor.Items.Add(CreateColorItem("Amber Yellow", "#9e7b10"));
            
            foreach (ComboBoxItem item in cbColor.Items)
            {
                if (item.Tag.ToString() == col.ColorHex)
                {
                    cbColor.SelectedItem = item;
                    break;
                }
            }

            formStack.Children.Add(cbColor);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 80;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Guardar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnSave.Width = 80;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    ShowCustomMessageBox("Introduce el nombre de la columna.", "Error", MessageBoxButton.OK);
                    return;
                }

                col.Name = txtName.Text.Trim();
                
                var selItem = cbColor.SelectedItem as ComboBoxItem;
                if (selItem != null)
                {
                    col.ColorHex = selItem.Tag.ToString();
                }

                Storage.Save(db);
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                RefreshWorkspace();
            }
        }

        private void DeleteColumn(KanbanColumn col)
        {
            if (activeProject.Columns.Count <= 1)
            {
                ShowCustomMessageBox("No puedes eliminar la única columna del proyecto.", "Aviso", MessageBoxButton.OK);
                return;
            }

            var result = ShowCustomMessageBox(
                "¿Seguro que deseas eliminar la columna \"" + col.Name + "\"? Las tareas asociadas se moverán a la primera columna.",
                "Eliminar Columna",
                MessageBoxButton.YesNo
            );

            if (result == MessageBoxResult.Yes)
            {
                activeProject.Columns.Remove(col);
                var firstCol = activeProject.Columns[0];
                
                foreach (var task in db.Tasks.Where(t => t.ProjectId == activeProject.Id && t.Status == col.Id))
                {
                    task.Status = firstCol.Id;
                }

                Storage.Save(db);
                RefreshWorkspace();
            }
        }

        private void DuplicateColumn(KanbanColumn originalCol)
        {
            if (originalCol == null || activeProject == null) return;

            var newCol = new KanbanColumn
            {
                Id = Guid.NewGuid().ToString(),
                Name = originalCol.Name + " (Copia)",
                ColorHex = originalCol.ColorHex
            };

            int idx = activeProject.Columns.IndexOf(originalCol);
            if (idx >= 0)
            {
                activeProject.Columns.Insert(idx + 1, newCol);
            }
            else
            {
                activeProject.Columns.Add(newCol);
            }

            // Copy all tasks in this column
            var originalTasks = db.Tasks.Where(t => t.ProjectId == activeProject.Id && t.Status == originalCol.Id).ToList();
            foreach (var task in originalTasks)
            {
                var newSubTasks = new List<SubTask>();
                if (task.SubTasks != null)
                {
                    foreach (var sub in task.SubTasks)
                    {
                        newSubTasks.Add(new SubTask
                        {
                            Title = sub.Title,
                            Completed = sub.Completed
                        });
                    }
                }

                var newTask = new DeveloperTask
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = activeProject.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    Tags = task.Tags,
                    Assignee = task.Assignee,
                    Link = task.Link,
                    Deadline = task.Deadline,
                    Status = newCol.Id,
                    Notified = false,
                    SubTasks = newSubTasks
                };
                db.Tasks.Add(newTask);
            }

            Storage.Save(db);
            RefreshWorkspace();
        }

        private void ShowAddColumnDialog()
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 360,
                Height = 265,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            border.Child = grid;

            var titleText = new TextBlock { Text = "Nueva Columna", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextActive, Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(titleText);
            Grid.SetRow(titleText, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = "Nombre de la Columna", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtName = new TextBox { Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtName);

            formStack.Children.Add(new TextBlock { Text = "Color de la Columna", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var cbColor = new ComboBox { FontSize = 11 };
            cbColor.Items.Add(CreateColorItem("Coral Red", "#962828"));
            cbColor.Items.Add(CreateColorItem("Emerald Green", "#1b6b3c"));
            cbColor.Items.Add(CreateColorItem("Ocean Blue", "#105b9e"));
            cbColor.Items.Add(CreateColorItem("Dark Teal", "#146455"));
            cbColor.Items.Add(CreateColorItem("Dark Purple", "#5a1e78"));
            cbColor.Items.Add(CreateColorItem("Sunset Orange", "#ac5616"));
            cbColor.Items.Add(CreateColorItem("Amber Yellow", "#9e7b10"));
            cbColor.SelectedIndex = 0;
            formStack.Children.Add(cbColor);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 80;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Guardar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnSave.Width = 80;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    ShowCustomMessageBox("Introduce el nombre de la columna.", "Error", MessageBoxButton.OK);
                    return;
                }

                string colorHex = "#962828";
                var selItem = cbColor.SelectedItem as ComboBoxItem;
                if (selItem != null)
                {
                    colorHex = selItem.Tag.ToString();
                }

                var newCol = new KanbanColumn
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = txtName.Text.Trim(),
                    ColorHex = colorHex
                };

                activeProject.Columns.Add(newCol);
                Storage.Save(db);
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                RefreshWorkspace();
            }
        }

        private void ShowTaskDialog(DeveloperTask task)
        {
            bool isEdit = task != null;

            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 440,
                Height = 410,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            border.Child = grid;

            var title = new TextBlock { Text = isEdit ? "Editar Tarea" : "Nueva Tarea", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextActive, Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(title);
            Grid.SetRow(title, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = "Título de la Tarea", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtTitle = new TextBox { Text = isEdit ? task.Title : "", Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), Margin = new Thickness(0, 0, 0, 10), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtTitle);

            formStack.Children.Add(new TextBlock { Text = "Descripción", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtDesc = new TextBox { Text = isEdit ? task.Description : "", Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), Margin = new Thickness(0, 0, 0, 10), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtDesc);

            // Columns row for priority and tags
            var colGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            formStack.Children.Add(colGrid);

            var prStack = new StackPanel();
            prStack.Children.Add(new TextBlock { Text = "Prioridad", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var cbPriority = new ComboBox { };
            cbPriority.Items.Add("Low");
            cbPriority.Items.Add("Medium");
            cbPriority.Items.Add("High");
            cbPriority.SelectedItem = isEdit ? task.Priority : "Medium";
            prStack.Children.Add(cbPriority);
            colGrid.Children.Add(prStack);
            Grid.SetColumn(prStack, 0);

            var tgStack = new StackPanel();
            tgStack.Children.Add(new TextBlock { Text = "Etiquetas (ej: iOS, Web)", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtTags = new TextBox { Text = isEdit ? task.Tags : "", Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), FontSize = 11.5, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            tgStack.Children.Add(txtTags);
            colGrid.Children.Add(tgStack);
            Grid.SetColumn(tgStack, 2);

            // Columns row for Assignee and Link
            var colGrid2 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            colGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            colGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            formStack.Children.Add(colGrid2);

            var asStack = new StackPanel();
            asStack.Children.Add(new TextBlock { Text = "Responsable", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtAssignee = new TextBox { Text = isEdit ? task.Assignee : "", Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), FontSize = 11.5, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            asStack.Children.Add(txtAssignee);
            colGrid2.Children.Add(asStack);
            Grid.SetColumn(asStack, 0);

            var lnStack = new StackPanel();
            lnStack.Children.Add(new TextBlock { Text = "Enlace de Referencia", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtLink = new TextBox { Text = isEdit ? task.Link : "", Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), FontSize = 11.5, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            lnStack.Children.Add(txtLink);
            colGrid2.Children.Add(lnStack);
            Grid.SetColumn(lnStack, 2);

            // Deadline and status row
            var colGrid3 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            colGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            colGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            colGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formStack.Children.Add(colGrid3);

            var dlStack = new StackPanel();
            dlStack.Children.Add(new TextBlock { Text = "Fecha Límite (ej: YYYY-MM-DD)", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtDeadline = new TextBox
            {
                Text = isEdit && task.Deadline != DateTime.MinValue ? task.Deadline.ToString("yyyy-MM-dd HH:mm") : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm"),
                Background = BgMain,
                Foreground = TextActive,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                FontSize = 11.5,
                CaretBrush = Brushes.White,
                SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204))
            };
            dlStack.Children.Add(txtDeadline);
            colGrid3.Children.Add(dlStack);
            Grid.SetColumn(dlStack, 0);

            ComboBox cbStatus = null;
            if (isEdit)
            {
                var stStack = new StackPanel();
                stStack.Children.Add(new TextBlock { Text = "Estado", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
                cbStatus = new ComboBox { };
                
                foreach (var col in activeProject.Columns)
                {
                    cbStatus.Items.Add(col.Name);
                }
                
                var currentCol = activeProject.Columns.FirstOrDefault(c => c.Id == task.Status);
                cbStatus.SelectedItem = currentCol != null ? currentCol.Name : activeProject.Columns[0].Name;
                
                stStack.Children.Add(cbStatus);
                colGrid3.Children.Add(stStack);
                Grid.SetColumn(stStack, 2);
            }

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 85;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Guardar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnSave.Width = 85;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtTitle.Text.Trim()))
                {
                    ShowCustomMessageBox("Por favor ingresa un título para la tarea.", "Error", MessageBoxButton.OK);
                    return;
                }

                DateTime deadlineVal = DateTime.MinValue;
                if (!string.IsNullOrEmpty(txtDeadline.Text.Trim()))
                {
                    if (!DateTime.TryParse(txtDeadline.Text.Trim(), out deadlineVal))
                    {
                        ShowCustomMessageBox("Formato de fecha inválido. Utiliza AAAA-MM-DD HH:MM.", "Error Fecha", MessageBoxButton.OK);
                        return;
                    }
                }

                if (isEdit)
                {
                    task.Title = txtTitle.Text.Trim();
                    task.Description = txtDesc.Text.Trim();
                    task.Priority = cbPriority.SelectedItem.ToString();
                    task.Tags = txtTags.Text.Trim();
                    task.Assignee = txtAssignee.Text.Trim();
                    task.Link = txtLink.Text.Trim();
                    task.Deadline = deadlineVal;
                    if (cbStatus != null)
                    {
                        var selCol = activeProject.Columns.FirstOrDefault(c => c.Name == cbStatus.SelectedItem.ToString());
                        task.Status = selCol != null ? selCol.Id : activeProject.Columns[0].Id;
                    }
                    
                    if (task.Deadline > DateTime.Now)
                    {
                        task.Notified = false;
                    }
                }
                else
                {
                    var newTask = new DeveloperTask
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProjectId = activeProject.Id,
                        Title = txtTitle.Text.Trim(),
                        Description = txtDesc.Text.Trim(),
                        Priority = cbPriority.SelectedItem.ToString(),
                        Tags = txtTags.Text.Trim(),
                        Assignee = txtAssignee.Text.Trim(),
                        Link = txtLink.Text.Trim(),
                        Deadline = deadlineVal,
                        Status = activeProject.Columns.Count > 0 ? activeProject.Columns[0].Id : "ToDo",
                        Notified = false
                    };
                    db.Tasks.Add(newTask);
                }

                Storage.Save(db);
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                RefreshWorkspace();
            }
        }

        // Helper: Create customized sleek close/minimize buttons
        private Button CreateTitleButton(string geometry, string tooltip, RoutedEventHandler clickHandler, bool isCloseButton = false)
        {
            var btn = new Button
            {
                Width = 46,
                Height = 42,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = WpfVectorIcons.GetIcon(geometry, new SolidColorBrush(Color.FromRgb(150, 150, 150)), 12),
                ToolTip = tooltip,
                Cursor = Cursors.Hand
            };

            var hoverBg = isCloseButton ? new SolidColorBrush(Color.FromRgb(232, 17, 35)) : new SolidColorBrush(Color.FromRgb(60, 60, 60));
            var hoverFg = Brushes.White;

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = hoverBg;
                ((System.Windows.Shapes.Path)btn.Content).Fill = hoverFg;
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                ((System.Windows.Shapes.Path)btn.Content).Fill = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            };

            btn.Click += clickHandler;

            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btn, true);

            return btn;
        }

        // Helper: Create uniform flat action button
        public static Button CreateFlatButton(string text, Brush normalBg, Brush hoverBg, Brush fg)
        {
            var btn = new Button
            {
                Content = text,
                Background = normalBg,
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Style = null
            };

            // Custom border rendering overrides default button style
            var templateBorder = new Border { Background = normalBg, CornerRadius = new CornerRadius(5), Child = new ContentPresenter { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            
            btn.MouseEnter += (s, e) => btn.Background = hoverBg;
            btn.MouseLeave += (s, e) => btn.Background = normalBg;

            return btn;
        }

        // Custom styled modern dark message box dialog
        public MessageBoxResult ShowCustomMessageBox(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var result = MessageBoxResult.OK;

            this.Dispatcher.Invoke(() =>
            {
                var win = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 380,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ShowInTaskbar = false
                };

                var border = new Border
                {
                    Background = BgSidebar,
                    BorderBrush = BorderColor,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20),
                    Margin = new Thickness(10),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Direction = 270,
                        ShadowDepth = 5,
                        Opacity = 0.5,
                        BlurRadius = 15
                    }
                };
                win.Content = border;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Message
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons
                border.Child = grid;

                var txtTitle = new TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextActive,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                grid.Children.Add(txtTitle);
                Grid.SetRow(txtTitle, 0);

                var txtMsg = new TextBlock
                {
                    Text = message,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 15)
                };
                grid.Children.Add(txtMsg);
                Grid.SetRow(txtMsg, 1);

                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                grid.Children.Add(btnPanel);
                Grid.SetRow(btnPanel, 2);

                if (buttons == MessageBoxButton.YesNo)
                {
                    var btnYes = CreateFlatButton("Sí", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
                    btnYes.Width = 70;
                    btnYes.Margin = new Thickness(0, 0, 10, 0);
                    btnYes.Click += (s, e) => { result = MessageBoxResult.Yes; win.DialogResult = true; };
                    btnPanel.Children.Add(btnYes);

                    var btnNo = CreateFlatButton("No", new SolidColorBrush(Color.FromRgb(150, 40, 40)), new SolidColorBrush(Color.FromRgb(180, 50, 50)), TextActive);
                    btnNo.Width = 70;
                    btnNo.Click += (s, e) => { result = MessageBoxResult.No; win.DialogResult = false; };
                    btnPanel.Children.Add(btnNo);
                }
                else
                {
                    var btnOk = CreateFlatButton("Aceptar", new SolidColorBrush(Color.FromRgb(0, 122, 204)), new SolidColorBrush(Color.FromRgb(30, 150, 240)), TextActive);
                    btnOk.Width = 80;
                    btnOk.Click += (s, e) => { result = MessageBoxResult.OK; win.DialogResult = true; };
                    btnPanel.Children.Add(btnOk);
                }

                win.ShowDialog();
            });

            return result;
        }

        public enum ImportConflictOption
        {
            Replace,
            CreateCopy,
            Cancel
        }

        private ImportConflictOption ShowImportConflictDialog(string projectName)
        {
            ImportConflictOption result = ImportConflictOption.Cancel;

            var dlg = new Window
            {
                Title = "Conflicto de Proyecto",
                Width = 480,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = false,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                ResizeMode = ResizeMode.NoResize,
                Topmost = true
            };

            var root = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };
            dlg.Content = root;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Desc
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Buttons
            root.Child = grid;

            var txtTitle = new TextBlock
            {
                Text = "⚠️ Conflicto de Importación",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            grid.Children.Add(txtTitle);
            Grid.SetRow(txtTitle, 0);

            var txtDesc = new TextBlock
            {
                Text = string.Format("El proyecto '{0}' ya existe en tu espacio de trabajo.\n¿Cómo deseas proceder?", projectName),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            grid.Children.Add(txtDesc);
            Grid.SetRow(txtDesc, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var btnReplace = CreateFlatButton("Reemplazar", new SolidColorBrush(Color.FromRgb(180, 40, 40)), new SolidColorBrush(Color.FromRgb(210, 50, 50)), TextActive);
            btnReplace.Width = 100;
            btnReplace.Margin = new Thickness(0, 0, 10, 0);
            btnReplace.Click += (s, e) => { result = ImportConflictOption.Replace; dlg.Close(); };

            var btnCopy = CreateFlatButton("Crear Copia", new SolidColorBrush(Color.FromRgb(0, 122, 204)), new SolidColorBrush(Color.FromRgb(30, 150, 240)), TextActive);
            btnCopy.Width = 100;
            btnCopy.Margin = new Thickness(0, 0, 10, 0);
            btnCopy.Click += (s, e) => { result = ImportConflictOption.CreateCopy; dlg.Close(); };

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(60, 60, 60)), new SolidColorBrush(Color.FromRgb(80, 80, 80)), TextActive);
            btnCancel.Width = 90;
            btnCancel.Click += (s, e) => { result = ImportConflictOption.Cancel; dlg.Close(); };

            btnPanel.Children.Add(btnReplace);
            btnPanel.Children.Add(btnCopy);
            btnPanel.Children.Add(btnCancel);

            grid.Children.Add(btnPanel);
            Grid.SetRow(btnPanel, 2);

            dlg.ShowDialog();
            return result;
        }

        private UIElement CreateMenuBarControl()
        {
            var menuPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                Height = 34,
                VerticalAlignment = VerticalAlignment.Center
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(menuPanel, true);

            var btnFile = CreateMenuButton("Archivo", (s, e) => ShowFileContextMenu(s as FrameworkElement));
            var btnEdit = CreateMenuButton("Editar", (s, e) => ShowEditContextMenu(s as FrameworkElement));
            var btnView = CreateMenuButton("Ver", (s, e) => ShowViewContextMenu(s as FrameworkElement));
            var btnTools = CreateMenuButton("Herramientas", (s, e) => ShowToolsContextMenu(s as FrameworkElement));
            var btnHelp = CreateMenuButton("Ayuda", (s, e) => ShowHelpContextMenu(s as FrameworkElement));

            menuPanel.Children.Add(btnFile);
            menuPanel.Children.Add(btnEdit);
            menuPanel.Children.Add(btnView);
            menuPanel.Children.Add(btnTools);
            menuPanel.Children.Add(btnHelp);

            return menuPanel;
        }

        private Button CreateMenuButton(string text, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(4, 2, 0, 2),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btn, true);

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 48)), "border"));
            triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            template.Triggers.Add(triggerHover);

            btn.Template = template;
            btn.Click += onClick;
            return btn;
        }

        private void ShowFileContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(cm, true);
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemExportAll = new MenuItem { Header = "📦 Exportar Todo (.etn)...", Foreground = Brushes.White, FontSize = 12 };
            itemExportAll.Click += (s, e) => ExportAllEnPackage();
            cm.Items.Add(itemExportAll);

            var itemExportActive = new MenuItem { Header = "📁 Exportar Proyecto Activo (.etn)...", Foreground = Brushes.White, FontSize = 12 };
            itemExportActive.Click += (s, e) => ExportActiveProjectEnPackage();
            cm.Items.Add(itemExportActive);

            var itemImport = new MenuItem { Header = "📥 Importar Archivo (.etn)...", Foreground = Brushes.White, FontSize = 12 };
            itemImport.Click += (s, e) => ImportEnPackage();
            cm.Items.Add(itemImport);

            cm.Items.Add(new Separator());

            var itemSave = new MenuItem { Header = "💾 Guardar Base de Datos", Foreground = Brushes.White, FontSize = 12 };
            itemSave.Click += (s, e) =>
            {
                Storage.Save(db);
                ShowCustomMessageBox("Guardado", "Base de datos guardada correctamente.", MessageBoxButton.OK);
            };
            cm.Items.Add(itemSave);

            cm.Items.Add(new Separator());

            var itemExit = new MenuItem { Header = "❌ Salir", Foreground = Brushes.White, FontSize = 12 };
            itemExit.Click += (s, e) => Application.Current.Shutdown();
            cm.Items.Add(itemExit);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowEditContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(cm, true);
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemNewProj = new MenuItem { Header = "➕ Nuevo Proyecto...", Foreground = Brushes.White, FontSize = 12 };
            itemNewProj.Click += (s, e) => ShowAddProjectDialog();
            cm.Items.Add(itemNewProj);

            var itemNewTask = new MenuItem { Header = "📝 Nueva Tarea...", Foreground = Brushes.White, FontSize = 12 };
            itemNewTask.Click += (s, e) => ShowTaskDialog(null);
            cm.Items.Add(itemNewTask);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowViewContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(cm, true);
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemKanban = new MenuItem { Header = "📊 Vista Kanban (Default)", IsChecked = true, Foreground = Brushes.White, FontSize = 12 };
            cm.Items.Add(itemKanban);

            var itemToggleSidebar = new MenuItem { Header = "👁️ Alternar Barra Lateral", Foreground = Brushes.White, FontSize = 12 };
            itemToggleSidebar.Click += (s, e) => ToggleSidebar();
            cm.Items.Add(itemToggleSidebar);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowToolsContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(cm, true);
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemRawJson = new MenuItem { Header = "⚙️ Exportar Backup RAW JSON...", Foreground = Brushes.White, FontSize = 12 };
            itemRawJson.Click += (s, e) => ExportRawJsonBackup();
            cm.Items.Add(itemRawJson);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowHelpContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(cm, true);
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemShortcuts = new MenuItem { Header = "⌨️ Atajos de Teclado", Foreground = Brushes.White, FontSize = 12 };
            itemShortcuts.Click += (s, e) => ShowShortcutsDialog();
            cm.Items.Add(itemShortcuts);

            var itemAbout = new MenuItem { Header = "ℹ️ Acerca de Etern-Notes", Foreground = Brushes.White, FontSize = 12 };
            itemAbout.Click += (s, e) => ShowAboutDialog();
            cm.Items.Add(itemAbout);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ExportAllEnPackage()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Etern Notes Package (*.etn)|*.etn;*.en;*.json|Todos los archivos (*.*)|*.*",
                DefaultExt = ".etn",
                FileName = string.Format("EternNotes_Full_Backup_{0}.etn", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    EternPackageHelper.ExportToEnFile(sfd.FileName, db.Projects, db.Tasks);
                    ShowCustomMessageBox(
                        "Exportación Exitosa",
                        string.Format("¡Exportación completada!\n\nSe han guardado todos los proyectos y tareas en:\n{0}", sfd.FileName),
                        MessageBoxButton.OK
                    );
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error de Exportación", "Error al exportar paquete .etn:\n" + ex.Message, MessageBoxButton.OK);
                }
            }
        }

        private void ExportActiveProjectEnPackage()
        {
            if (activeProject == null)
            {
                ShowCustomMessageBox("Aviso", "No hay ningún proyecto activo para exportar.", MessageBoxButton.OK);
                return;
            }

            var activeTasks = db.Tasks.Where(t => t != null && t.ProjectId == activeProject.Id).ToList();
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Etern Notes Package (*.etn)|*.etn;*.en;*.json|Todos los archivos (*.*)|*.*",
                DefaultExt = ".etn",
                FileName = string.Format("{0}.etn", activeProject.Name.Replace(" ", "_"))
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    EternPackageHelper.ExportToEnFile(sfd.FileName, new List<Project> { activeProject }, activeTasks);
                    ShowCustomMessageBox(
                        "Exportación Exitosa",
                        string.Format("¡Proyecto '{0}' exportado correctamente!\n\nArchivo guardado en:\n{1}", activeProject.Name, sfd.FileName),
                        MessageBoxButton.OK
                    );
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error de Exportación", "Error al exportar paquete .etn:\n" + ex.Message, MessageBoxButton.OK);
                }
            }
        }

        private void ImportEnPackage()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Etern Notes Package (*.etn;*.en;*.json)|*.etn;*.en;*.json|Todos los archivos (*.*)|*.*",
                DefaultExt = ".etn"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var package = EternPackageHelper.ReadEnFile(ofd.FileName);
                    if (package.Projects == null || package.Projects.Count == 0)
                    {
                        ShowCustomMessageBox("Importación", "El archivo no contiene ningún proyecto válido.", MessageBoxButton.OK);
                        return;
                    }

                    int importedCount = 0;
                    var importedProjects = package.Projects.ToList();

                    foreach (var importedProj in importedProjects)
                    {
                        if (importedProj == null) continue;

                        var importedTasks = (package.Tasks ?? new List<DeveloperTask>())
                            .Where(t => t != null && t.ProjectId == importedProj.Id)
                            .ToList();

                        var existingProj = db.Projects.FirstOrDefault(p => p != null && p.Name.Equals(importedProj.Name, StringComparison.OrdinalIgnoreCase));
                        if (existingProj != null)
                        {
                            var conflictResult = ShowImportConflictDialog(importedProj.Name);
                            if (conflictResult == ImportConflictOption.Replace)
                            {
                                string targetId = existingProj.Id;
                                db.Projects.Remove(existingProj);
                                db.Tasks.RemoveAll(t => t != null && t.ProjectId == targetId);

                                db.Projects.Add(importedProj);
                                foreach (var task in importedTasks)
                                {
                                    db.Tasks.Add(task);
                                }
                                activeProject = importedProj;
                                importedCount++;
                            }
                            else if (conflictResult == ImportConflictOption.CreateCopy)
                            {
                                string newId = Guid.NewGuid().ToString();

                                importedProj.Id = newId;
                                importedProj.Name = importedProj.Name + " (Copia)";

                                foreach (var task in importedTasks)
                                {
                                    task.Id = Guid.NewGuid().ToString();
                                    task.ProjectId = newId;
                                    db.Tasks.Add(task);
                                }

                                db.Projects.Add(importedProj);
                                activeProject = importedProj;
                                importedCount++;
                            }
                            // If Cancel, skip
                        }
                        else
                        {
                            db.Projects.Add(importedProj);
                            foreach (var task in importedTasks)
                            {
                                db.Tasks.Add(task);
                            }
                            activeProject = importedProj;
                            importedCount++;
                        }
                    }

                    if (importedCount > 0)
                    {
                        Storage.Save(db);
                        RefreshProjects();
                        RefreshWorkspace();
                        ShowCustomMessageBox("Importación Exitosa", string.Format("¡Se han importado {0} proyecto(s) correctamente!", importedCount), MessageBoxButton.OK);
                    }
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error de Importación", "No se pudo importar el archivo:\n" + ex.Message, MessageBoxButton.OK);
                }
            }
        }

        private void ExportRawJsonBackup()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = string.Format("EternNotes_RawBackup_{0}.json", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    Storage.Save(db);
                    string rawJson = File.ReadAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EternNotes", "data.json"));
                    File.WriteAllText(sfd.FileName, rawJson);
                    ShowCustomMessageBox("Backup Guardado", "Copia RAW JSON exportada en:\n" + sfd.FileName, MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error", "Error al exportar JSON:\n" + ex.Message, MessageBoxButton.OK);
                }
            }
        }

        private void ShowShortcutsDialog()
        {
            ShowCustomMessageBox(
                "⌨️ Atajos de Teclado",
                "• F11: Alternar Pantalla Completa\n" +
                "• Ctrl + S: Guardar Base de Datos\n" +
                "• Ctrl + E: Exportar Paquete .etn\n" +
                "• Ctrl + I: Importar Paquete .etn\n" +
                "• Esc: Cerrar Diálogos",
                MessageBoxButton.OK
            );
        }

        private void ShowAboutDialog()
        {
            ShowCustomMessageBox(
                "ℹ️ Acerca de Etern-Notes",
                "🚀 Etern-Notes v1.2 (Native Cross-Platform Workspace)\n\n" +
                "Desarrollado para la gestión eficiente de proyectos y tareas.\n" +
                "• Formato de Paquetes: .etn (Etern Notes Package)\n" +
                "• Licencia: MIT\n" +
                "• Desarrollador: paucg06\n\n" +
                "© 2026 Etern Studio.",
                MessageBoxButton.OK
            );
        }
    }

    // Custom Slide-Up Alert Toast Window
    public class ToastWindow : Window
    {
        private DispatcherTimer slideTimer;
        private DispatcherTimer displayTimer;
        private double targetY;
        private double slideSpeed = 10;
        private int state = 0; // 0: sliding up, 1: displaying, 2: sliding down
        private DeveloperTask task;
        private Action<DeveloperTask> onCompleted;

        public ToastWindow(DeveloperTask task, Action<DeveloperTask> onCompleted)
        {
            this.task = task;
            this.onCompleted = onCompleted;

            // Window Setup
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 320;
            Height = 110;

            // Core Layout Border
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true
            };
            this.Content = border;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) }); // Priority line indicator
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Main text
            border.Child = grid;

            // Priority accent line
            var accentColor = new SolidColorBrush(Color.FromRgb(56, 139, 253)); // Blue
            if (task.Priority == "High") accentColor = new SolidColorBrush(Color.FromRgb(215, 58, 73)); // Red
            else if (task.Priority == "Medium") accentColor = new SolidColorBrush(Color.FromRgb(227, 179, 65)); // Yellow/Orange

            var accentBar = new Border
            {
                Background = accentColor,
                CornerRadius = new CornerRadius(5, 0, 0, 5)
            };
            grid.Children.Add(accentBar);
            Grid.SetColumn(accentBar, 0);

            var mainArea = new Grid { Margin = new Thickness(12, 10, 12, 10) };
            grid.Children.Add(mainArea);
            Grid.SetColumn(mainArea, 1);

            mainArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Task Name
            mainArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Actions

            // Header Title
            var txtTitle = new TextBlock
            {
                Text = "⚠️ Límite de Tarea Cercano",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            };
            mainArea.Children.Add(txtTitle);
            Grid.SetRow(txtTitle, 0);

            // Close button X
            var txtClose = new TextBlock
            {
                Text = "×",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, -4, 0, 0)
            };
            txtClose.MouseEnter += (s, e) => txtClose.Foreground = Brushes.White;
            txtClose.MouseLeave += (s, e) => txtClose.Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120));
            txtClose.MouseDown += (s, e) => Dismiss();
            mainArea.Children.Add(txtClose);
            Grid.SetRow(txtClose, 0);

            // Body layout containing task details
            var bodyStack = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };
            mainArea.Children.Add(bodyStack);
            Grid.SetRow(bodyStack, 1);

            var txtTaskName = new TextBlock
            {
                Text = task.Title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            bodyStack.Children.Add(txtTaskName);

            var txtRemaining = new TextBlock
            {
                Text = "Vence en menos de 6 horas (" + task.Deadline.ToString("HH:mm") + ")",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            bodyStack.Children.Add(txtRemaining);

            // Action "Completar" button
            var btnDone = MainWindow.CreateFlatButton("Completar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), Brushes.White);
            btnDone.FontSize = 10.5;
            btnDone.FontWeight = FontWeights.Bold;
            btnDone.Padding = new Thickness(8, 3, 8, 3);
            btnDone.HorizontalAlignment = HorizontalAlignment.Right;
            btnDone.VerticalAlignment = VerticalAlignment.Bottom;
            btnDone.Click += (s, e) =>
            {
                if (onCompleted != null)
                {
                    onCompleted(task);
                }
                Dismiss();
            };
            mainArea.Children.Add(btnDone);
            Grid.SetRow(btnDone, 2);

            // Set up placement coordinates: off-screen bottom-right
            double screenWidth = SystemParameters.WorkArea.Right;
            double screenHeight = SystemParameters.WorkArea.Bottom;

            this.Left = screenWidth - this.Width - 15;
            this.Top = screenHeight;
            targetY = screenHeight - this.Height - 15;

            // Setup movement logic timers
            slideTimer = new DispatcherTimer();
            slideTimer.Interval = TimeSpan.FromMilliseconds(10);
            slideTimer.Tick += SlideTimer_Tick;

            displayTimer = new DispatcherTimer();
            displayTimer.Interval = TimeSpan.FromSeconds(7);
            displayTimer.Tick += (s, e) => { displayTimer.Stop(); Dismiss(); };

            this.Loaded += (s, e) => slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            if (state == 0) // Sliding Up
            {
                if (this.Top > targetY)
                {
                    this.Top = Math.Max(targetY, this.Top - slideSpeed);
                }
                else
                {
                    slideTimer.Stop();
                    state = 1;
                    displayTimer.Start();
                }
            }
            else if (state == 2) // Sliding Down
            {
                double screenHeight = SystemParameters.WorkArea.Bottom;
                if (this.Top < screenHeight)
                {
                    this.Top = this.Top + slideSpeed;
                }
                else
                {
                    slideTimer.Stop();
                    this.Close();
                }
            }
        }

        private void Dismiss()
        {
            state = 2;
            slideTimer.Start();
        }

        // Prevent taking window focus on show
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            SetWindowNoActivate(helper.Handle);
        }

        // PInvoke to disable window activation so it doesn't steal focus
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private static void SetWindowNoActivate(IntPtr hwnd)
        {
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }
    }
}
