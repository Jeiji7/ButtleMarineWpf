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

        private bool isManualPlacementMode = false; // Режим ручной расстановки
        private int shipLength = 1; // Длина корабля по умолчанию
        private bool isHorizontal = true; // Ориентация корабля по умолчанию (горизонтально)

        private Dictionary<int, int> shipLimits = new Dictionary<int, int>
        {
            { 1, 4 }, // 4 однопалубных корабля
            { 2, 3 }, // 3 двухпалубных корабля
            { 3, 2 }, // 2 трёхпалубных корабля
            { 4, 1 }  // 1 четырёхпалубный корабль
        };

        private Dictionary<int, int> placedShips = new Dictionary<int, int>
        {
            { 1, 0 }, // Количество размещённых однопалубных кораблей
            { 2, 0 }, // Количество размещённых двухпалубных кораблей
            { 3, 0 }, // Количество размещённых трёхпалубных кораблей
            { 4, 0 }  // Количество размещённых четырёхпалубных кораблей
        };

        //public MainWindow()
        //{
        //    //InitializeComponent();
        //    //InitializeGrids();
        //    //PlaceShips(playerShips, playerButtons, true);
        //    //PlaceShips(enemyShips, enemyButtons, false);
        //}

        public MainWindow()
        {
            InitializeComponent();
            InitializeGrids();
        }

        private void ShipLengthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем длину корабля
            shipLength = int.Parse((ShipLengthComboBox.SelectedItem as ComboBoxItem).Content.ToString());
        }

        private void ToggleOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            // Меняем ориентацию корабля
            isHorizontal = !isHorizontal;
            ToggleOrientationButton.Content = isHorizontal ? "Горизонтально" : "Вертикально";
        }

        private void AutoPlacementButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем поля
            ClearGrids();

            // Инициализируем сетки
            InitializeGrids();

            // Расставляем корабли автоматически
            PlaceShips(playerShips, playerButtons, true);
            PlaceShips(enemyShips, enemyButtons, false);

            // Активируем кнопку "Старт"
            StartButton.IsEnabled = true;
            isManualPlacementMode = false;
            MoveHistoryList.Items.Clear();
        }

        private void ManualPlacementButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем поля
            ClearGrids();

            // Инициализируем сетки
            InitializeGrids();

            // Активируем режим ручной расстановки
            isManualPlacementMode = true;
            StartButton.IsEnabled = false; // Кнопка "Старт" будет активирована после завершения расстановки
            PlaceShips(enemyShips, enemyButtons, false);
            MoveHistoryList.Items.Clear();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Начинаем игру
            isPlayerTurn = true;
            MessageBox.Show("Игра началась!");
        }

        private void InitializeGrids()
        {
            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    // Создаём кнопку для игрока
                    playerButtons[i, j] = new Button();
                    playerButtons[i, j].Click += PlayerButton_Click; // Добавляем обработчик клика
                    Grid.SetColumn(playerButtons[i, j], i); // Устанавливаем столбец
                    Grid.SetRow(playerButtons[i, j], j);    // Устанавливаем строку
                    PlayerGrid.Children.Add(playerButtons[i, j]);

                    // Создаём кнопку для противника
                    enemyButtons[i, j] = new Button();
                    enemyButtons[i, j].Click += EnemyButton_Click;
                    buttonCoordinates[enemyButtons[i, j]] = (i, j);
                    Grid.SetColumn(enemyButtons[i, j], i); // Устанавливаем столбец
                    Grid.SetRow(enemyButtons[i, j], j);    // Устанавливаем строку
                    EnemyGrid.Children.Add(enemyButtons[i, j]);
                }
            }
        }

        private void PlayerGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isManualPlacementMode) return;

            // Получаем координаты клика
            Point position = e.GetPosition(PlayerGrid);
            int x = (int)(position.X / (PlayerGrid.ActualWidth / GridSize));
            int y = (int)(position.Y / (PlayerGrid.ActualHeight / GridSize));

            // Размещаем корабль
            if (CanPlaceShip(x, y, shipLength, isHorizontal, playerButtons))
            {
                PlaceShip(x, y, shipLength, isHorizontal, playerButtons, playerShips);
            }

            // Активируем кнопку "Старт" после завершения расстановки
            StartButton.IsEnabled = true;
        }

        private void PlaceShip(int x, int y, int length, bool horizontal, Button[,] buttons, List<Ship> ships)
        {
            ships.Add(new Ship(x, y, length, horizontal));
            for (int i = 0; i < length; i++)
            {
                int xi = x + (horizontal ? i : 0);
                int yi = y + (horizontal ? 0 : i);
                buttons[xi, yi].Background = Brushes.Blue; // Закрашиваем клетку корабля
                buttons[xi, yi].Tag = ships.Last(); // Связываем клетку с кораблём
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

                // Проверяем соседние клетки
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
        private void ClearGrids()
        {
            PlayerGrid.Children.Clear();
            EnemyGrid.Children.Clear();
            playerShips.Clear();
            enemyShips.Clear();
            playerButtons = new Button[GridSize, GridSize];
            enemyButtons = new Button[GridSize, GridSize];

            // Сбрасываем счётчики размещённых кораблей
            placedShips = new Dictionary<int, int>
            {
                { 1, 0 },
                { 2, 0 },
                { 3, 0 },
                { 4, 0 }
            };
        }

        private void PlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isManualPlacementMode) return;

            Button button = sender as Button;
            if (button == null) return;

            int x = Grid.GetColumn(button);
            int y = Grid.GetRow(button);

            if (placedShips[shipLength] >= shipLimits[shipLength])
            {
                MessageBox.Show($"Достигнут лимит кораблей длиной {shipLength}!");
                return;
            }

            if (CanPlaceShip(x, y, shipLength, isHorizontal, playerButtons))
            {
                PlaceShip(x, y, shipLength, isHorizontal, playerButtons, playerShips);
                placedShips[shipLength]++; // Увеличиваем количество размещённых кораблей
                UpdateShipCounts(); // Обновляем отображение количества кораблей
            }

            // Активируем кнопку "Старт", если все корабли размещены
            StartButton.IsEnabled = AreAllShipsPlaced();
        }

        private bool AreAllShipsPlaced()
        {
            foreach (var key in shipLimits.Keys)
            {
                if (placedShips[key] < shipLimits[key])
                    return false;
            }
            return true;
        }

        private void UpdateShipCounts()
        {
            Ship1Text.Text = $"1-палубные: {shipLimits[1] - placedShips[1]}";
            Ship2Text.Text = $"2-палубные: {shipLimits[2] - placedShips[2]}";
            Ship3Text.Text = $"3-палубные: {shipLimits[3] - placedShips[3]}";
            Ship4Text.Text = $"4-палубные: {shipLimits[4] - placedShips[4]}";
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
            bool allPlayerShipsSunk = playerShips.All(ship => ship.Hits == ship.Length);
            bool allEnemyShipsSunk = enemyShips.All(ship => ship.Hits == ship.Length);

            if (allEnemyShipsSunk)
            {
                MessageBox.Show("Вы победили!");
                ResetGame();
            }
            else if (allPlayerShipsSunk)
            {
                MessageBox.Show("Противник победил!");
                ResetGame();
            }
        }
        private void ResetGame()
        {
            // Очищаем поля и перезапускаем игру
            ClearGrids();
            InitializeGrids();
            UpdateShipCounts();
            PlaceShips(playerShips, playerButtons, true);
            PlaceShips(enemyShips, enemyButtons, false);
            isPlayerTurn = true; // Сбрасываем ход
            MoveHistoryList.Items.Clear();
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
