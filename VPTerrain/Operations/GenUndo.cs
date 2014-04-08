using System;
using System.Collections.Generic;
using System.IO;
using VP;

namespace VPTerrain
{
    public class GenUndo : IOperation
    {
        const string tag = "GenUndo";

        int      toDelete;
        int      deleted;
        bool     deleting;
        string   path;
        Instance bot;

        public void Run(Instance bot)
        {
            while (true)
            {
                path = ConsoleEx.Ask("What generator log do you wish to undo?").Trim('"');

                if ( !File.Exists(path) )
                    Log.Warn(tag, "No such file: {0}", path);
                else
                    break;
            }

            this.bot = bot;
            bot.Property.CallbackObjectDelete += onDelete;
            CommenceDelete();
        }

        public void CommenceDelete()
        {
            var lines = File.ReadAllLines(path);
            var ids   = new List<int>(lines.Length);
            deleted   = 0;
            deleting  = true;

            VPTerrain.Busy = true;

            foreach (var line in lines)
            {
                int id;

                if ( !int.TryParse(line, out id) )
                {
                    Log.Warn(tag, "Skipping non-integer line: {0}", line);
                    continue;
                }
                else
                    ids.Add(id);
            }

            Log.Info(tag, "Commencing undo of {0} objects...", ids.Count);
            toDelete = ids.Count;

            foreach (var id in ids)
                bot.Property.DeleteObject(id);

            while (deleting)
                bot.Pump();

            VPTerrain.Busy = false;
        }

        void onDelete(Instance sender, ReasonCode result, int id)
        {
            Console.Title = "Deleted object ID {0}; {1} out of {2}".LFormat(id, deleted, toDelete);
            deleted++;

            if (deleted >= toDelete)
            {
                Log.Info(tag, "All {0} objects undone", toDelete);
                deleting = false;
            }
        }

        public void Dispose()
        {
            bot.Property.CallbackObjectDelete -= onDelete;
        }
    }
}
