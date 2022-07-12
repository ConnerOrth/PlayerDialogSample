namespace PlayerDialogSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Questing System!");

            //Create a target for a quest
            Monster wolf = new Monster()
            {
                Name = "Wolf"
            };
            //create a quest
            Quest wolfSlayer = new Quest()
            {
                Name = "Bane of wolves",
            };
            //add the required monster to kill
            wolfSlayer.Requirements.Add(new Requirement()
            {
                IdOfRequiredThing = wolf.Id,
                RequiredAmount = 5
            });

            //some basic dialog with a quest
            Dialog dialog = new Dialog("Help, the wolves keep hunting my chickens!", null, new Dialog("Would you please get rid of those pesky wolves?", wolfSlayer, null));
            NPC chickenFarmer = new NPC();
            chickenFarmer.Dialogs.Add(dialog);

            Player mightyHero = new Player()
            {
                Name = "TheMightyHero"
            };

            //get dialog from npc.
            var currentDialog = chickenFarmer.Interact(mightyHero);


            //traverse the dialog
            Console.WriteLine(dialog.Text);
            Console.WriteLine("--Press any key to continue--");
            Console.Read();
            while (currentDialog.HasMoreDialog)
            {
                currentDialog = dialog.NextDialog();
                Console.WriteLine(currentDialog.Text);
                Console.WriteLine("--Press any key to continue--");
                Console.Read();
                //check for quest
                if (currentDialog.HasQuest)
                {
                    Console.WriteLine("--Press Y to accept the quest");
                    //accept or decline quest
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        var quest = currentDialog.AcceptQuest();
                        QuestManager.Instance.AddQuest(mightyHero, quest);
                    }
                    else
                    {
                        Console.WriteLine("--The mighty hero has decided to decline--");
                        Console.WriteLine("This worlds needs a true hero, exiting program.");
                        Environment.Exit(0);
                    }
                }
            }

            //keep game running till we completed the quest
            while (!QuestManager.Instance.IsMainQuestCompleted(mightyHero))
            {
                Console.WriteLine("--Press Y to slay a wolf--");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    //trigger oncombatexit to update the quest
                    mightyHero.OnCombatExit?.Invoke(wolf);
                }
            }

            Console.WriteLine();
            Console.WriteLine("--Thank you for saving the poor chickenfarmer-- ");
        }
    }
    public class Monster
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
    public class QuestManager
    {
        private static readonly Lazy<QuestManager> lazy = new Lazy<QuestManager>(() => new QuestManager());
        public static QuestManager Instance { get { return lazy.Value; } }

        private IDictionary<Player, IList<Quest>> playerQuests = new Dictionary<Player, IList<Quest>>();
        private QuestManager() { }


        public void AddQuest(Player player, Quest quest)
        {
            //if we dont have a questlog yet create it.
            if (!playerQuests.ContainsKey(player))
            {
                playerQuests.Add(player, new List<Quest>());
            }
            //if quest hasnt been added to questlog yet, add it.
            if (!playerQuests[player].Any(q => q.Id == quest.Id))
            {
                playerQuests[player].Add(quest);
                Console.WriteLine($"{player.Name} accepted {quest.Name}");
                player.OnCombatExit += (monster) => UpdateQuest(player, quest, monster);
            }
        }

        private void UpdateQuest(Player player, Quest quest, Monster monster)
        {
            //try to get the requirement for this monster/quest/player
            var requirement = playerQuests[player].FirstOrDefault(q => q.Id == quest.Id).Requirements.FirstOrDefault(r => r.IdOfRequiredThing == monster.Id);
            if (requirement == null) return;
            requirement.CurrentAmount += 1;
            if (quest.IsCompleted)
            {
                Console.WriteLine($"{player.Name} has completed Quest: {quest.Name}");
            }
            else
            {
                Console.WriteLine($"Requirement updated: {requirement.CurrentAmount}/{requirement.RequiredAmount}");
            }
            Console.WriteLine();
        }
        public bool IsMainQuestCompleted(Player player)
        {
            return playerQuests[player].First().IsCompleted;
        }
    }
    public class Player
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Player_One";
        public Action<Monster> OnCombatExit { get; set; }
        public Player()
        {
            OnCombatExit += (monster) => Console.WriteLine($"{Name} has just slain a {monster.Name}.");
        }
    }
    public class NPC
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public IList<Dialog> Dialogs { get; set; } = new List<Dialog>();
        public Dialog Interact(Player player)
        {
            return Dialogs.First();
        }
    }

    public class Dialog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; }
        public Quest? Quest { get; set; }
        public bool HasQuest => Quest != null;
        private Dialog? dialog { get; set; }
        public bool HasMoreDialog => dialog != null;
        public Dialog(string text, Quest? quest, Dialog? dialog)
        {
            Text = text;
            Quest = quest;
            this.dialog = dialog;
        }

        public Dialog NextDialog()
        {
            if (!HasMoreDialog) throw new Exception("Can't ask for more dialog of there is none.");
            return dialog!;
        }
        public Quest AcceptQuest()
        {
            if (!HasQuest) throw new Exception("You can't accept a quest that is no quest is present.");
            return Quest!;
        }
    }
    public class Quest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public IList<Requirement> Requirements { get; set; } = new List<Requirement>();
        public bool IsCompleted => Requirements.All(r => r.Completed);
    }
    public class Requirement
    {
        public int RequiredAmount { get; set; }
        public int CurrentAmount { get; set; }
        public bool Completed => CurrentAmount >= RequiredAmount;
        public Guid IdOfRequiredThing { get; set; }
    }
}