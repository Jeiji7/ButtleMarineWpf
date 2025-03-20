using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace ButtleMarineWpf
{

    public partial class MainWindow : Window
    {
        private const int GridSize = 10;
        private Button[,] playerButtons = new Button[GridSize, GridSize];
        private Button[,] enemyButtons = new Button[GridSize, GridSize];
        private List<Ship> playerShips = new List<Ship>();
        private List<Ship> enemyShips = new List<Ship>();
        private Random rand = new Random();
        private bool isPlayerTurn = true; // Переменная для отслеживания хода

        private int lastHitX = -1; // Координата X последнего успешного выстрела
        private int lastHitY = -1; // Координата Y последнего успешного выстрела
        private bool isSearching = false; // Флаг, указывающий, что бот ищет корабль
        private List<(int dx, int dy)> directions = new List<(int, int)> { (0, 1), (1, 0), (0, -1), (-1, 0) }; // Направления для стрельбы
        private int currentDirectionIndex = 0; // Текущее направление стрельбы
        private bool isDestroying = false; // Флаг, указывающий, что бот уничтожает корабль
        private int initialHitX = -1; // Начальная координата X попадания
        private int initialHitY = -1; // Начальная координата Y попадания
        private int shipDirectionX = 0; // Направление корабля по X (1, -1, 0)
        private int shipDirectionY = 0; // Направление корабля по Y (1, -1, 0)

        private Dictionary<Button, (int, int)> buttonCoordinates = new Dictionary<Button, (int, int)>();
        public MainWindow()
        {
            InitializeComponent();
            InitializeGrids();
            PlaceShips(playerShips, playerButtons, true);
            PlaceShips(enemyShips, enemyButtons, false);
        }

        private void InitializeGrids()
        {
            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    playerButtons[i, j] = new Button();
                    PlayerGrid.Children.Add(playerButtons[i, j]);

                    enemyButtons[i, j] = new Button();
                    enemyButtons[i, j].Click += EnemyButton_Click;
                    buttonCoordinates[enemyButtons[i, j]] = (i, j);
                    EnemyGrid.Children.Add(enemyButtons[i, j]);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем поля и перезапускаем игру
            PlayerGrid.Children.Clear();
            EnemyGrid.Children.Clear();
            playerShips.Clear();
            enemyShips.Clear();
            InitializeGrids();
            PlaceShips(playerShips, playerButtons, true);
            PlaceShips(enemyShips, enemyButtons, false);
            isPlayerTurn = true; // Сбрасываем ход
            MoveHistoryList.Items.Clear();
        }

        private void PlaceShips(List<Ship> ships, Button[,] buttons, bool isPlayer)
        {
            int[] shipLengths = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            foreach (int length in shipLengths)
            {
                bool placed = false;
                while (!placed)
                {
                    int x = rand.Next(GridSize);
                    int y = rand.Next(GridSize);
                    bool horizontal = rand.Next(2) == 0;

                    if (CanPlaceShip(x, y, length, horizontal, buttons))
                    {
                        ships.Add(new Ship(x, y, length, horizontal));
                        for (int i = 0; i < length; i++)
                        {
                            int xi = x + (horizontal ? i : 0);
                            int yi = y + (horizontal ? 0 : i);
                            buttons[xi, yi].Tag = ships.Last();
                            if (isPlayer)
                            {
                                buttons[xi, yi].Background = System.Windows.Media.Brushes.Blue;
                            }
                        }
                        placed = true;
                    }
                }
            }
        }
        private bool CanPlaceShip(int x, int y, int length, bool horizontal, Button[,] buttons)
        {
            for (int i = 0; i < length; i++)
            {
                int xi = x + (horizontal ? i : 0);
                int yi = y + (horizontal ? 0 : i);
                if (xi >= GridSize || yi >= GridSize || buttons[xi, yi].Tag != null)
                {
                    return false;
                }



            for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int xj = xi + dx;
                        int yj = yi + dy;
                        if (xj >= 0 && xj < GridSize && yj >= 0 && yj < GridSize && buttons[xj, yj].Tag != null)
                        {
                            return false;
                        }
                    }
                }

            }
            return true;
        }

        private void EnemyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayerTurn) return; // Если ход не игрока, ничего не делаем

            Button button = (Button)sender;

            // Проверяем, можно ли стрелять в эту клетку
            if (button.Background == System.Windows.Media.Brushes.Red ||
                button.Background == System.Windows.Media.Brushes.DarkGray ||
                button.Tag?.ToString() == "Forbidden")
            {
                return; // Клетка уже атакована, ничего не делаем
            }
            if (sender is Button button1 && buttonCoordinates.ContainsKey(button1))
            {
                (int x, int y) = buttonCoordinates[button1];
                AddMoveToHistory("Игрок", y, x);
            }

            //var (x, y) = ((int, int))
            //// Добавляем запись о ходе игрока
            //AddMoveToHistory("Игрок", y, x);
            if (button.Tag is Tuple<int, int> coordinates)
            {
                int x = coordinates.Item1;
                int y = coordinates.Item2;

                AddMoveToHistory("Игрок", y, x);
                MessageBox.Show($"Вы нажали на кнопку с координатами: {x}, {y}");
            }

            if (button.Tag is Ship ship)
            {
                button.Background = System.Windows.Media.Brushes.Red;
                button.Content = "X"; // Добавляем крестик при попадании
                ship.Hits++;
                if (ship.Hits == ship.Length)
                {
                    MarkSunkenShip(ship, enemyButtons); // Помечаем зону вокруг уничтоженного корабля
                }
                isPlayerTurn = true; // Игрок продолжает ход
            }
            else
            {
                button.Background = System.Windows.Media.Brushes.DarkGray; // Закрашиваем промах
                button.Content = "•"; // Добавляем точку при промахе
                button.Tag = "Forbidden"; // Помечаем клетку как недоступную для выстрела
                isPlayerTurn = false; // Передаём ход боту
                BotAttack(); // Бот атакует
            }

            CheckGameOver();
        }

        private bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && x < GridSize && y >= 0 && y < GridSize;
        }
        private void BotAttack()
        {
            bool hit = false;
            do
            {
                int x, y;

                if (isSearching && lastHitX != -1 && lastHitY != -1)
                {
                    if (isDestroying)
                    {
                        // Бот продолжает стрелять в том же направлении, пока не промахнется
                        x = lastHitX + shipDirectionX;
                        y = lastHitY + shipDirectionY;

                        // Если вышли за пределы поля или клетка уже атакована, возвращаемся к начальной точке
                        if (!IsWithinBounds(x, y) ||
                            playerButtons[x, y].Background == System.Windows.Media.Brushes.Red ||
                            playerButtons[x, y].Background == System.Windows.Media.Brushes.LightGray ||
                            playerButtons[x, y].Tag?.ToString() == "Forbidden")
                        {
                            // Возвращаемся к начальной точке и меняем направление на противоположное
                            x = initialHitX - shipDirectionX;
                            y = initialHitY - shipDirectionY;

                            // Если противоположная клетка уже атакована или выходит за пределы, сбрасываем состояние
                            if (!IsWithinBounds(x, y) ||
                                playerButtons[x, y].Background == System.Windows.Media.Brushes.Red ||
                                playerButtons[x, y].Background == System.Windows.Media.Brushes.LightGray ||
                                playerButtons[x, y].Tag?.ToString() == "Forbidden")
                            {
                                isSearching = false; // Сбрасываем режим поиска
                                isDestroying = false; // Сбрасываем режим уничтожения
                                lastHitX = -1;
                                lastHitY = -1;
                                initialHitX = -1;
                                initialHitY = -1;
                                shipDirectionX = 0;
                                shipDirectionY = 0;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // Бот ищет корабль, стреляя в соседние клетки
                        (int dx, int dy) = directions[currentDirectionIndex];
                        x = lastHitX + dx;
                        y = lastHitY + dy;

                        // Если вышли за пределы поля или клетка уже атакована, меняем направление
                        if (!IsWithinBounds(x, y) ||
                            playerButtons[x, y].Background == System.Windows.Media.Brushes.Red ||
                            playerButtons[x, y].Background == System.Windows.Media.Brushes.LightGray ||
                            playerButtons[x, y].Tag?.ToString() == "Forbidden")
                        {
                            currentDirectionIndex = (currentDirectionIndex + 1) % directions.Count; // Меняем направление
                            continue;
                        }
                    }
                }
                else
                {
                    // Бот стреляет случайно
                    do
                    {
                        x = rand.Next(GridSize);
                        y = rand.Next(GridSize);
                    } while (playerButtons[x, y].Background == System.Windows.Media.Brushes.Red ||
                             playerButtons[x, y].Background == System.Windows.Media.Brushes.LightGray ||
                             playerButtons[x, y].Tag?.ToString() == "Forbidden");
                }

                Button button = playerButtons[x, y];

                // Добавляем запись о ходе бота
                AddMoveToHistory("Бот", y, x);

                if (button.Tag is Ship ship)
                {
                    button.Background = System.Windows.Media.Brushes.Red;
                    button.Content = "X"; // Добавляем крестик при попадании
                    ship.Hits++;
                    if (ship.Hits == ship.Length)
                    {
                        MarkSunkenShip(ship, playerButtons); // Помечаем зону вокруг уничтоженного корабля
                        isSearching = false; // Сбрасываем логику поиска
                        isDestroying = false; // Сбрасываем режим уничтожения
                        lastHitX = -1;
                        lastHitY = -1;
                        initialHitX = -1;
                        initialHitY = -1;
                        shipDirectionX = 0;
                        shipDirectionY = 0;
                    }
                    else
                    {
                        isSearching = true; // Бот продолжает искать корабль
                        if (isDestroying)
                        {
                            // Если бот уже уничтожает корабль, обновляем последние координаты
                            lastHitX = x;
                            lastHitY = y;
                        }
                        else
                        {
                            // Если это второе попадание, переходим в режим уничтожения
                            isDestroying = true;
                            lastHitX = x;
                            lastHitY = y;
                            initialHitX = x; // Запоминаем начальную точку
                            initialHitY = y;
                            shipDirectionX = x - initialHitX; // Запоминаем направление корабля
                            shipDirectionY = y - initialHitY;
                        }
                    }
                    hit = true; // Бот попал, продолжает ход
                }
                else
                {
                    button.Background = System.Windows.Media.Brushes.DarkGray; // Закрашиваем промах бота
                    button.Content = "•"; // Добавляем точку при промахе
                    button.Tag = "Forbidden"; // Помечаем клетку как недоступную для выстрела
                    if (isSearching)
                    {
                        // Если бот промахнулся, меняем направление
                        if (isDestroying)
                        {
                            isDestroying = false; // Сбрасываем режим уничтожения
                            currentDirectionIndex = (currentDirectionIndex + 2) % directions.Count; // Меняем направление на противоположное
                        }
                        else
                        {
                            currentDirectionIndex = (currentDirectionIndex + 1) % directions.Count; // Меняем направление
                        }
                    }
                    hit = false; // Бот не попал, ход переходит игроку
                }
            } while (hit); // Бот продолжает атаковать, пока попадает

            isPlayerTurn = true; // Передаём ход игроку
            CheckGameOver();
        }

        private void MarkSunkenShip(Ship ship, Button[,] buttons)
        {
            // Проверяем, что корабль действительно уничтожен
            if (ship.Hits != ship.Length)
            {
                return; // Корабль ещё не уничтожен, ничего не делаем
            }

            for (int i = 0; i < ship.Length; i++)
            {
                int xi = ship.X + (ship.Horizontal ? i : 0);
                int yi = ship.Y + (ship.Horizontal ? 0 : i);

                // Помечаем клетки вокруг корабля
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int xj = xi + dx;
                        int yj = yi + dy;

                        // Проверяем, чтобы координаты были в пределах поля
                        if (xj >= 0 && xj < GridSize && yj >= 0 && yj < GridSize)
                        {
                            Button button = buttons[xj, yj];

                            // Если клетка ещё не была атакована, помечаем её
                            if (button.Background != System.Windows.Media.Brushes.Red &&
                                button.Background != System.Windows.Media.Brushes.DarkGray)
                            {
                                button.Background = System.Windows.Media.Brushes.DarkGray; // Закрашиваем зону
                                button.Content = "•"; // Ставим точку
                                button.Tag = "Forbidden"; // Помечаем клетку как недоступную для выстрела
                            }
                        }
                    }
                }
            }
        }

        private void CheckGameOver()
        {
            if (playerShips.All(ship => ship.Hits == ship.Length))
            {
                MessageBox.Show("Противник победил!");
            }
            else if (enemyShips.All(ship => ship.Hits == ship.Length))
            {
                MessageBox.Show("Вы победили!");
            }
            
        }
        private void AddMoveToHistory(string playerName, int x, int y)
        {
            // Преобразуем координаты в буквенно-числовой формат (например, 1,A)
            char column = (char)('A' + x);
            int row = y + 1;

            // Формируем строку для отображения
            string moveInfo = $"{playerName}: {row}{column}";

            // Добавляем строку в ListBox
            MoveHistoryList.Items.Add(moveInfo);

            // Прокручиваем ListBox к последнему элементу
            MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[MoveHistoryList.Items.Count - 1]);
        }
    }
    //BB
    public class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
        public bool Horizontal { get; }
        public int Hits { get; set; }

        public Ship(int x, int y, int length, bool horizontal)
        {
            X = x;
            Y = y;
            Length = length;
            Horizontal = horizontal;
            Hits = 0;
        }
    }
}
