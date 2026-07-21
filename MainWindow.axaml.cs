using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;

namespace EternNotes
{
    public partial class MainWindow : Window
    {
        private Database db;
        private Project activeProject;

        public MainWindow()
        {
            InitializeComponent();
            db = Storage.Load();
            if (db.Projects.Count > 0)
            {
                activeProject = db.Projects[0];
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            RefreshProjects();
            RefreshKanban();
        }

        private void RefreshProjects()
        {
            var projectListPanel = this.FindControl<StackPanel>("ProjectListPanel");
            if (projectListPanel == null) return;

            projectListPanel.Children.Clear();
            foreach (var proj in db.Projects)
            {
                bool isActive = activeProject != null && activeProject.Id == proj.Id;
                var btn = new Button
                {
                    Content = "📁  " + proj.Name,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = isActive ? SolidColorBrush.Parse("#2A2A2A") : SolidColorBrush.Parse("Transparent"),
                    Foreground = isActive ? Brushes.White : SolidColorBrush.Parse("#AAAAAA"),
                    Padding = new Thickness(10, 8),
                    CornerRadius = new CornerRadius(4),
                    Tag = proj.Id
                };

                btn.Click += (s, e) =>
                {
                    activeProject = proj;
                    RefreshUI();
                };

                projectListPanel.Children.Add(btn);
            }
        }

        private void RefreshKanban()
        {
            var txtTitle = this.FindControl<TextBlock>("TxtProjectTitle");
            var txtDesc = this.FindControl<TextBlock>("TxtProjectDesc");
            var kanbanGrid = this.FindControl<Grid>("KanbanGrid");

            if (txtTitle == null || txtDesc == null || kanbanGrid == null) return;

            if (activeProject == null)
            {
                txtTitle.Text = "Sin Proyecto";
                txtDesc.Text = "Crea un proyecto para empezar";
                kanbanGrid.Children.Clear();
                return;
            }

            txtTitle.Text = activeProject.Name;
            txtDesc.Text = activeProject.Description;

            kanbanGrid.Children.Clear();
            kanbanGrid.ColumnDefinitions.Clear();

            var columns = activeProject.Columns.Count > 0 ? activeProject.Columns : new List<KanbanColumn>
            {
                new KanbanColumn { Id = "ToDo", Name = "POR HACER", ColorHex = "#962828" },
                new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" },
                new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5A1E78" }
            };

            int colIdx = 0;
            foreach (var col in columns)
            {
                kanbanGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

                var colBorder = new Border
                {
                    Background = SolidColorBrush.Parse("#1A1A1A"),
                    BorderBrush = SolidColorBrush.Parse("#303030"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 15, 0),
                    Padding = new Thickness(12)
                };

                var colPanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };

                // Header
                var headerText = new TextBlock
                {
                    Text = col.Name,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13,
                    Foreground = SolidColorBrush.Parse(col.ColorHex ?? "#007ACC"),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                colPanel.Children.Add(headerText);
                Grid.SetRow(headerText, 0);

                // Cards Scrollable Panel
                var tasksPanel = new StackPanel { Spacing = 10 };
                var projTasks = db.Tasks.Where(t => t.ProjectId == activeProject.Id && t.Status == col.Id).ToList();

                foreach (var task in projTasks)
                {
                    var card = CreateTaskCard(task);
                    tasksPanel.Children.Add(card);
                }

                var scrollViewer = new ScrollViewer { Content = tasksPanel };
                colPanel.Children.Add(scrollViewer);
                Grid.SetRow(scrollViewer, 1);

                colBorder.Child = colPanel;
                kanbanGrid.Children.Add(colBorder);
                Grid.SetColumn(colBorder, colIdx);

                colIdx++;
            }
        }

        private Control CreateTaskCard(DeveloperTask task)
        {
            var cardBorder = new Border
            {
                Background = SolidColorBrush.Parse("#212121"),
                BorderBrush = SolidColorBrush.Parse("#333333"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12)
            };

            var cardStack = new StackPanel { Spacing = 6 };

            // Title
            var txtTitle = new TextBlock
            {
                Text = task.Title,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            cardStack.Children.Add(txtTitle);

            // Description
            if (!string.IsNullOrEmpty(task.Description))
            {
                var txtDesc = new TextBlock
                {
                    Text = task.Description,
                    FontSize = 12,
                    Foreground = SolidColorBrush.Parse("#AAAAAA"),
                    TextWrapping = TextWrapping.Wrap
                };
                cardStack.Children.Add(txtDesc);
            }

            // Priority badge & Actions
            var footerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

            string priorityColor = task.Priority == "High" ? "#E74C3C" : (task.Priority == "Medium" ? "#F39C12" : "#2ECC71");
            var badge = new Border
            {
                Background = SolidColorBrush.Parse(priorityColor),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = task.Priority,
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                }
            };
            footerGrid.Children.Add(badge);
            Grid.SetColumn(badge, 0);

            // Move status button
            var btnNext = new Button
            {
                Content = "➔",
                Background = SolidColorBrush.Parse("#333333"),
                Foreground = Brushes.White,
                Padding = new Thickness(6, 2),
                FontSize = 11,
                CornerRadius = new CornerRadius(3)
            };
            btnNext.Click += (s, e) => MoveTaskNext(task);
            footerGrid.Children.Add(btnNext);
            Grid.SetColumn(btnNext, 1);

            cardStack.Children.Add(footerGrid);
            cardBorder.Child = cardStack;

            return cardBorder;
        }

        private void MoveTaskNext(DeveloperTask task)
        {
            if (activeProject == null) return;
            var cols = activeProject.Columns.Count > 0 ? activeProject.Columns : new List<KanbanColumn>
            {
                new KanbanColumn { Id = "ToDo", Name = "POR HACER" },
                new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO" },
                new KanbanColumn { Id = "Done", Name = "COMPLETADO" }
            };

            int currentIdx = cols.FindIndex(c => c.Id == task.Status);
            if (currentIdx >= 0 && currentIdx < cols.Count - 1)
            {
                task.Status = cols[currentIdx + 1].Id;
                Storage.Save(db);
                RefreshKanban();
            }
            else if (currentIdx == cols.Count - 1)
            {
                task.Status = cols[0].Id;
                Storage.Save(db);
                RefreshKanban();
            }
        }

        private void BtnNewProject_Click(object sender, RoutedEventArgs e)
        {
            var p = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Nuevo Proyecto",
                Description = "Descripción del proyecto",
                IconType = "Folder",
                Columns = new List<KanbanColumn>
                {
                    new KanbanColumn { Id = "ToDo", Name = "POR HACER", ColorHex = "#962828" },
                    new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" },
                    new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5A1E78" }
                }
            };
            db.Projects.Add(p);
            activeProject = p;
            Storage.Save(db);
            RefreshUI();
        }

        private void BtnNewTask_Click(object sender, RoutedEventArgs e)
        {
            if (activeProject == null) return;

            var t = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = activeProject.Id,
                Title = "Nueva Tarea",
                Description = "Detalles de la tarea...",
                Status = activeProject.Columns.Count > 0 ? activeProject.Columns[0].Id : "ToDo",
                Priority = "Medium",
                Deadline = DateTime.Now.AddDays(1)
            };

            db.Tasks.Add(t);
            Storage.Save(db);
            RefreshKanban();
        }

        private async void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Exportar Todo (*.en)",
                DefaultExtension = "en",
                InitialFileName = $"EternNotes_Full_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.en"
            };
            dlg.Filters.Add(new FileDialogFilter { Name = "Etern Notes Package", Extensions = new List<string> { "en" } });

            var result = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    EternPackageHelper.ExportToEnFile(result, db.Projects, db.Tasks);
                }
                catch (Exception) { }
            }
        }

        private async void ExportActive_Click(object sender, RoutedEventArgs e)
        {
            if (activeProject == null) return;
            var dlg = new SaveFileDialog
            {
                Title = $"Exportar {activeProject.Name} (*.en)",
                DefaultExtension = "en",
                InitialFileName = $"{activeProject.Name.Replace(" ", "_")}.en"
            };
            dlg.Filters.Add(new FileDialogFilter { Name = "Etern Notes Package", Extensions = new List<string> { "en" } });

            var result = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    var activeTasks = db.Tasks.Where(t => t.ProjectId == activeProject.Id).ToList();
                    EternPackageHelper.ExportToEnFile(result, new List<Project> { activeProject }, activeTasks);
                }
                catch (Exception) { }
            }
        }

        private async void ImportEn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Importar Archivo (*.en)",
                AllowMultiple = false
            };
            dlg.Filters.Add(new FileDialogFilter { Name = "Etern Notes Package", Extensions = new List<string> { "en", "json" } });

            var results = await dlg.ShowAsync(this);
            if (results != null && results.Length > 0)
            {
                try
                {
                    var package = EternPackageHelper.ReadEnFile(results[0]);
                    foreach (var importedProj in package.Projects)
                    {
                        var importedTasks = package.Tasks.Where(t => t.ProjectId == importedProj.Id).ToList();
                        db.Projects.Add(importedProj);
                        foreach (var task in importedTasks)
                        {
                            db.Tasks.Add(task);
                        }
                        activeProject = importedProj;
                    }
                    Storage.Save(db);
                    RefreshUI();
                }
                catch (Exception) { }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Storage.Save(db);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
