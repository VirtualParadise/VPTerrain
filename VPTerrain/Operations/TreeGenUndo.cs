using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using VP;

namespace VPTerrain
{
    public class TreeGenUndo : IOperation
    {
        const string tag = "TreeGenUndo";

        int      toDelete;
        int      deleted;
        bool     deleting;
        string   path;
        Instance bot;

        public void Run(Instance bot)
        {
            while (true)
            {
                path = ConsoleEx.Ask("What treegenlog do you wish to undo?").Trim('"');

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

            Log.Info(tag, "Commencing undo of {0} trees...", ids.Count);
            toDelete = ids.Count;

            foreach (var id in ids)
                bot.Property.DeleteObject(id);

            while (deleting)
                bot.Wait(0);
        }

        void onDelete(Instance sender, ObjectCallbackData args)
        {
            Console.Title = "Deleted tree ID {0}; {1} out of {2}".LFormat(args.Object.Id, deleted, toDelete);
            deleted++;

            if (deleted >= toDelete)
            {
                Log.Info(tag, "All {0} trees undone", toDelete);
                deleting = false;
            }
        }

        public void Dispose()
        {
            bot.Property.CallbackObjectDelete -= onDelete;
        }
    }
}
