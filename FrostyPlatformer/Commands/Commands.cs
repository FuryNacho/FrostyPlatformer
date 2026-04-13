using FrostyPlatformer.Models.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyPlatformer.Commands
{
    public class Command
    {
        public Command()
        {
            Completed = false;
            Started = false;
        }

        public bool Completed { get; set; }
        public bool Started { get; set; }
        public Quest? Quest { get; set; }
        public virtual void Start() { }
        public virtual void Update(float elapsedTime) { }

        public virtual void AddQuest(Quest quest)
        {
        }

        //public static Program Engine { get; set; }
        public static Program Engine { get
            {
                return Core.Aggregate.Instance.ThisGame!;
            }
        }

    }

    public class ScriptProcessor
    {
        public ScriptProcessor()
        {
            UserControlEnabled = true;
        }

        private List<Command> listCommands { get; set; } = new List<Command>();

        public bool UserControlEnabled { get; set; }

        public virtual void AddCommand(Command cmd)
        {
            listCommands.Add(cmd);
        }

        public void ProcessCommands(float elapsedTime)
        {
            UserControlEnabled = !listCommands.Any(); // finns inget i listan ge kontroll till spelaren

            if (!UserControlEnabled) // Om det finns nåt i listan, gör nåt
            {
                if (!listCommands.FirstOrDefault()!.Completed) // Om inte den första processen i listan är klar
                {
                    if (!listCommands.FirstOrDefault()!.Started)// Om första objektet inte är startad, starta den
                    {
                        listCommands.FirstOrDefault()!.Start();
                        listCommands.FirstOrDefault()!.Started = true;
                    }
                    else
                    {
                        // currently in process
                        listCommands.FirstOrDefault()!.Update(elapsedTime);
                    }

                }
                else
                {
                    //Command has been completed
                    listCommands.RemoveAt(0);
                }
            }

        }

        public void CompletedCommand()
        {
            if (listCommands.Any())
            {
                listCommands.FirstOrDefault()!.Completed = true;
            }
        }
    }


    public class CommandMoveTo : Command
    {
        // target x and y position in world space, duration hur fort det ska gå
        public CommandMoveTo(DynamicGameObject myObject, float x, float y, float duration = 0.0f)
        {
            TargetPosX = x;
            TargetPosY = y;
            TimeSoFar = 0.0f;
            Duration = Math.Max(duration, 0.001f);
            this.myObject = myObject;
        }

        public override void Start()
        {
            StartPosX = myObject.px;
            StartPosY = myObject.py;
        }
        public override void Update(float elapsedTime)
        {
            // linger interpolation
            TimeSoFar += elapsedTime;
            float t = TimeSoFar / Duration;
            if (t > 1.0f)
            {
                t = 1.0f;
            }

            // speed = distance over time
            myObject.px = (TargetPosX - StartPosX) * t + StartPosX;
            myObject.py = (TargetPosY - StartPosY) * t + StartPosY;
            myObject.vx = (TargetPosX - StartPosX) / Duration;
            myObject.vy = (TargetPosY - StartPosY) / Duration;

            if (TimeSoFar >= Duration)
            {
                //Object has reached destination, sp stop
                myObject.px = TargetPosX;
                myObject.py = TargetPosY;
                myObject.vx = 0.0f;
                myObject.vy = 0.0f;
                Completed = true;
            }

        }


        // Klassen tar kontroll över objektet.
        private DynamicGameObject myObject { get; set; }
        // startposition
        public float StartPosX { get; set; }
        public float StartPosY { get; set; }
        //Slutposition
        public float TargetPosX { get; set; }
        public float TargetPosY { get; set; }
        // Store value for the duration
        public float Duration { get; set; }
        //time passed so far
        public float TimeSoFar { get; set; }

    }




    public class CommandShowDialog : Command
    {
        public CommandShowDialog(List<string> line)
        {
            listLines = line;
        }

        public List<string> listLines { get; set; } = new List<string>();

        public override void Start()
        {
            Engine.ShowDialog(listLines);
        }


    }

    public class CommandChangeMap : Command
    {
        public CommandChangeMap(string mapName, float mapPosX, float mapPosY)
        {
            MapName = mapName;
            MapPosX = mapPosX;
            MapPosY = mapPosY;
        }

        public override void Start()
        {
            Engine.ChangeMap(MapName, MapPosX, MapPosY);
            Completed = true;
        }

        public string MapName { get; set; }
        public float MapPosX { get; set; }
        public float MapPosY { get; set; }
    }



    // Någonstans i slutet på 3.. hängde inte med på vad han gjorde

    public class CommandAddQuest : Command
    {
        public CommandAddQuest(Quest quest)
        {
            m_quest = quest;
        }

        private Quest m_quest { get; set; }
        public override void Start()
        {
            Engine.AddQuest(m_quest);
            Completed = true;
        }
    }
}
