using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MyApp
{
    public class Program
    {
        public static List<Client> clients = new List<Client>();
        public static object locker = new Object();
        public static bool isChanged;
        public static int idChanged;
        public static decimal lastChanged;
        public static void Main(string[] args)
        {
            Timer timer = new Timer(Checker, null, 0, 2_000);
            while (true)
            {
                System.Console.WriteLine("");
                foreach (var s in new string[] { "(1)-Insert", "(2)-Update", "(3)-Delete", "(4)-Select", " _ -Exit" })
                    System.Console.WriteLine(s);
                int.TryParse(Console.ReadLine(), out int command);
                if (command < 1 || command > 4) return;
                ParameterizedThreadStart callback;
                object par = null;
                switch (command)
                {
                    case 1:
                        callback = Insert;
                        break;
                    case 2:
                        callback = Update;
                        int id = GetID();
                        System.Console.Write("Укажите сумму изменения : ");
                        decimal.TryParse(Console.ReadLine(), out decimal summ);
                        par = new Client(id, summ);
                        break;
                    case 3:
                        callback = Delete;
                        par = GetID();
                        break;
                    default:
                        callback = Select;
                        break;
                }
                Thread thread = new Thread(callback);
                thread.Start(par);
                thread.Join();
            }
        }
        public static int GetID()
        {
            Select();
            System.Console.Write("Укажите ID : ");
            int.TryParse(Console.ReadLine(), out int id);
            if (clients.Contains((clients.Find(x => x.ID == id)))) return id;
            System.Console.WriteLine("Не правильный ID");
            return -1;
        }
        public static void Insert(object _ = null)
        {
            lock (locker)
                clients.Add(new Client());
        }
        public static void Update(object obj)
        {
            if (!(obj is Client temp)) return;
            lock (locker)
                clients.Find(x => x.ID == temp.ID)?.AddBalance(temp.Balance);
        }
        public static void Delete(object id)
        {
            lock (locker)
                clients.Remove(clients.Find(x => x.ID == (id as int? ?? 0)));
        }
        public static void Select(object _ = null)
        {
            lock (locker)
            {
                Table table = new Table("ID", "Before", "After", "Difference");
                foreach (var client in clients)
                    table.AddRaw(client.ID.ToString(), client.BalanceBefore.ToString(), client.Balance.ToString(), client.Difference.ToString());
                table.ShowTable();
            }
        }
        public static void Checker(object _)
        {
            if (!isChanged) return;
            isChanged = false;
            Client client = clients.Find(x => x.ID == idChanged);
            Console.ForegroundColor = lastChanged >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Table table = new Table("ID", "Before", "After", "Difference");
            bool isReduce = client.Difference < lastChanged;
            table.AddRaw(idChanged.ToString(), client.BalanceBefore.ToString(), client.Balance.ToString(), (isReduce ? "(-)" : "(+)") + client.Difference.ToString());
            table.ShowTable(isReduce ? ConsoleColor.Red : ConsoleColor.Green);
        }
    }
    public class Client
    {
        private static int StaticID { get; set; } = 0;
        public int ID { get; }
        public decimal Balance { get; private set; } = 0;
        public decimal BalanceBefore { get; private set; } = 0;
        public decimal Difference { get; set; } = 0;
        public Client() => ID = ++StaticID;
        public Client(int id, decimal bal)
        {
            ID = id;
            Balance = bal;
        }
        public void AddBalance(decimal money)
        {
            Program.isChanged = true;
            Program.idChanged = ID;
            Program.lastChanged = Difference;
            BalanceBefore = Balance;
            Balance += money;
            Difference = money;
        }
    }
    class Table
    {
        private string[][] table;
        private int[] maxLRaw;
        private int raws = 0;
        private readonly int cols;
        public Table(params string[] firstRaw)
        {
            cols = firstRaw.Length;
            maxLRaw = new int[cols];
            for (int i = 0; i < cols; i++)
                maxLRaw[i] = 0;
            AddRaw(firstRaw);
        }
        public void AddRaw(params string[] raw)
        {
            Array.Resize<string[]>(ref table, raws + 1);
            table[^1] = new string[cols];
            for (int i = 0; i < cols; i++)
            {
                table[raws][i] = raw[i];
                maxLRaw[i] = raw[i].Length > maxLRaw[i] ? raw[i].Length : maxLRaw[i];
            }
            raws++;
        }
        public void ShowTable(ConsoleColor color = ConsoleColor.Cyan)
        {
            Console.ForegroundColor = color;
            StringBuilder sb = new StringBuilder(Sum() + cols + 1);
            sb.Append('|').Append(' ', maxLRaw[0] - table[0][0].Length).Append(table[0][0]).Append('|');
            for (int i = 1; i < cols; i++)
                sb.Append(' ', maxLRaw[i] - table[0][i].Length).Append(table[0][i]).Append('|');
            System.Console.WriteLine(sb);
            sb.Clear();
            sb.Append('|').Append('-', maxLRaw[0]).Append('|');
            for (int i = 1; i < cols; i++)
                sb.Append('-', maxLRaw[i]).Append('|');
            System.Console.WriteLine(sb);
            sb.Clear();
            for (int i = 1; i < raws; i++)
            {
                sb.Append('|').Append(' ', maxLRaw[0] - table[i][0].Length).Append(table[i][0]).Append('|');
                for (int j = 1; j < cols; j++)
                    sb.Append(' ', maxLRaw[j] - table[i][j].Length).Append(table[i][j]).Append('|');
                System.Console.WriteLine(sb);
                sb.Clear();
            }
            Console.ResetColor();
        }
        private int Sum()
        {
            int sum = 0;
            for (int i = 0; i < cols; i++)
                sum += ++maxLRaw[i];
            return sum;
        }
    }
}