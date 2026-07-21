using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DevPlanner
{
    [DataContract]
    public class KanbanColumn
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ColorHex { get; set; } // Hex accent color, e.g. "#962828"
    }

    [DataContract]
    public class Project
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string IconType { get; set; } // "Gamepad", "Video", "Folder"

        [DataMember]
        public List<KanbanColumn> Columns { get; set; }

        public Project()
        {
            Columns = new List<KanbanColumn>();
        }
    }

    [DataContract]
    public class SubTask
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public bool Completed { get; set; }
    }

    [DataContract]
    public class DeveloperTask
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string ProjectId { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Status { get; set; } // Maps to KanbanColumn.Id

        [DataMember]
        public string Priority { get; set; } // "High", "Medium", "Low"

        [DataMember]
        public DateTime Deadline { get; set; }

        [DataMember]
        public bool Notified { get; set; }

        [DataMember]
        public string Tags { get; set; } // e.g. "iOS, Web, Desktop"

        [DataMember]
        public string Assignee { get; set; }

        [DataMember]
        public string Link { get; set; }

        [DataMember]
        public List<SubTask> SubTasks { get; set; }

        public DeveloperTask()
        {
            SubTasks = new List<SubTask>();
        }
    }

    [DataContract]
    public class Database
    {
        [DataMember]
        public List<Project> Projects { get; set; }

        [DataMember]
        public List<DeveloperTask> Tasks { get; set; }

        public Database()
        {
            Projects = new List<Project>();
            Tasks = new List<DeveloperTask>();
        }
    }

    public static class Storage
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "EternNotes"
        );
        private static readonly string FilePath = Path.Combine(FolderPath, "data.json");

        public static Database Load()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                if (!File.Exists(FilePath))
                {
                    var db = CreateDefaultDatabase();
                    Save(db);
                    return db;
                }

                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Database));
                    var db = (Database)serializer.ReadObject(fs);
                    
                    if (db.Projects == null) db.Projects = new List<Project>();
                    if (db.Tasks == null) db.Tasks = new List<DeveloperTask>();
                    
                    foreach (var proj in db.Projects)
                    {
                        if (proj.Columns == null) proj.Columns = new List<KanbanColumn>();
                        if (proj.Columns.Count == 0)
                        {
                            proj.Columns.Add(new KanbanColumn { Id = "ToDo", Name = "PLANIFICADO", ColorHex = "#962828" });
                            proj.Columns.Add(new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" });
                            proj.Columns.Add(new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5a1e78" });
                        }
                    }

                    foreach (var task in db.Tasks)
                    {
                        if (task.SubTasks == null) task.SubTasks = new List<SubTask>();
                    }
                    
                    return db;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading data: " + ex.Message);
                return CreateDefaultDatabase();
            }
        }

        public static void Save(Database db)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Database));
                    serializer.WriteObject(fs, db);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving data: " + ex.Message);
            }
        }

        private static Database CreateDefaultDatabase()
        {
            var db = new Database();
            
            var p1 = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Indie Game Development",
                Description = "Desarrollo de un juego de plataformas 2D en Godot.",
                IconType = "Gamepad"
            };
            p1.Columns.Add(new KanbanColumn { Id = "ToDo", Name = "PLANIFICADO", ColorHex = "#962828" });
            p1.Columns.Add(new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" });
            p1.Columns.Add(new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5a1e78" });

            var p2 = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "YouTube Videos",
                Description = "Canal de tecnología y tutoriales de programación.",
                IconType = "Video"
            };
            p2.Columns.Add(new KanbanColumn { Id = "ToDo", Name = "PLANIFICADO", ColorHex = "#962828" });
            p2.Columns.Add(new KanbanColumn { Id = "InProgress", Name = "EN PROGRESO", ColorHex = "#146455" });
            p2.Columns.Add(new KanbanColumn { Id = "Done", Name = "COMPLETADO", ColorHex = "#5a1e78" });

            db.Projects.Add(p1);
            db.Projects.Add(p2);

            // Task 1
            var t1 = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = p1.Id,
                Title = "Diseñar Sprite del Personaje",
                Description = "Crear las animaciones de caminar, saltar y reposo.",
                Status = "ToDo",
                Priority = "Medium",
                Deadline = DateTime.Now.AddDays(2),
                Notified = false,
                Tags = "iOS, Web",
                Assignee = "Guna",
                Link = "https://notion.so/guna-sprites"
            };
            t1.SubTasks.Add(new SubTask { Title = "Bocetos de animación", Completed = true });
            t1.SubTasks.Add(new SubTask { Title = "Vectorizar y colorear", Completed = false });
            t1.SubTasks.Add(new SubTask { Title = "Exportar Spritesheet", Completed = false });
            db.Tasks.Add(t1);

            // Task 2
            var t2 = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = p1.Id,
                Title = "Implementar Movimiento Físico",
                Description = "Script del personaje con aceleración y gravedad.",
                Status = "InProgress",
                Priority = "High",
                Deadline = DateTime.Now.AddDays(1),
                Notified = false,
                Tags = "Desktop",
                Assignee = "Guna"
            };
            t2.SubTasks.Add(new SubTask { Title = "Cálculo de gravedad", Completed = true });
            t2.SubTasks.Add(new SubTask { Title = "Fricción del suelo", Completed = false });
            t2.SubTasks.Add(new SubTask { Title = "Control de salto variable", Completed = false });
            db.Tasks.Add(t2);

            // Task 3
            var t3 = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = p1.Id,
                Title = "Crear Tilemap de Prueba",
                Description = "Set de tiles básicos para testear el movimiento físico.",
                Status = "Done",
                Priority = "Low",
                Deadline = DateTime.Now.AddDays(-2),
                Notified = false,
                Tags = "Desktop",
                Assignee = "Guna"
            };
            t3.SubTasks.Add(new SubTask { Title = "Tiles de suelo y paredes", Completed = true });
            t3.SubTasks.Add(new SubTask { Title = "Importar a Godot", Completed = true });
            db.Tasks.Add(t3);

            // Task 4
            var t4 = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = p2.Id,
                Title = "Escribir Guion de Tutorial C#",
                Description = "Planificar la explicación de Programación Orientada a Objetos.",
                Status = "ToDo",
                Priority = "High",
                Deadline = DateTime.Now.AddHours(5),
                Notified = false,
                Tags = "Video, Web",
                Assignee = "Ana",
                Link = "https://notion.so/tutorial-csharp"
            };
            t4.SubTasks.Add(new SubTask { Title = "Estructura del video", Completed = false });
            t4.SubTasks.Add(new SubTask { Title = "Escribir ejemplos prácticos", Completed = false });
            db.Tasks.Add(t4);

            // Task 5
            var t5 = new DeveloperTask
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = p2.Id,
                Title = "Editar Video de Setup",
                Description = "Cortar tomas falsas y añadir música de fondo ligera.",
                Status = "InProgress",
                Priority = "Medium",
                Deadline = DateTime.Now.AddDays(3),
                Notified = false,
                Tags = "Video",
                Assignee = "Ana"
            };
            t5.SubTasks.Add(new SubTask { Title = "Corte preliminar de tomas", Completed = true });
            t5.SubTasks.Add(new SubTask { Title = "Ajuste de audio y música", Completed = false });
            db.Tasks.Add(t5);

            return db;
        }
    }
}
