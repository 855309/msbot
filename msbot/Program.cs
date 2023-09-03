using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace msbot
{
    internal class Program
    {
        // Define color arrays for various elements on the Minesweeper grid.
        static Color[] init_col = {
            Color.FromArgb(162, 209, 73),
            Color.FromArgb(170, 215, 81),
            Color.FromArgb(185, 221, 119)
        };

        static Color[] emp_col = {
            Color.FromArgb(225, 202, 179),
            Color.FromArgb(215, 184, 153),
            Color.FromArgb(229, 194, 159)
        };

        static Color[] num_col = {
            Color.FromArgb(25, 118, 210), // 1
            Color.FromArgb(56, 142, 60),  // 2
            Color.FromArgb(211, 47, 47),  // 3
            Color.FromArgb(123, 31, 162), // 4
            Color.FromArgb(255, 143, 0),  // 5
            Color.FromArgb(0, 151, 167)   // 6
        };

        static Color flag = Color.FromArgb(242, 54, 7);

        // Define profiles for different difficulty levels.
        /*
         * Easy:   p_x = 727, p_y = 406, g_w = 10, g_h = 8, b_w = 45, flagcount = 10
         * Medium: p_x = 682, p_y = 376, g_w = 18, g_h = 14, b_w = 30, flagcount = 40
         * Hard:   p_x = 652, p_y = 336, g_w = 24, g_h = 20, b_w = 25, flagcount = 99
         */
        static int[,] profiles = { { 727, 406, 10, 8, 45, 10 }, { 682, 376, 18, 14, 30, 40 }, { 652, 336, 24, 20, 25, 99 } };

        // Initialize variables for grid properties and settings.
        static int p_x, p_y, g_w, g_h, b_w, flagcount;
        static int anim_delay = 620;

        static int[,] grid;
        
        // Function to update the Minesweeper grid from the screen capture.
        static void update_grid()
        {
            // Capture the Minesweeper grid as a bitmap.
            Bitmap bitmap = new Bitmap(g_w * b_w, g_h * b_w);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(p_x, p_y), Point.Empty, new Size(g_w * b_w, g_h * b_w));
            }

            // Analyze the bitmap to determine the state of each cell on the grid.
            for (int x = 0; x < g_w; x++)
            {
                for (int y = 0; y < g_h; y++)
                {
                    int stx = x * b_w, sty = y * b_w;

                    bool fnd = false;
                    int col = 0;

                    // Check the color of a region within each cell to identify its state.
                    for (int i = stx + (b_w / 2) - (b_w / 4); i < stx + (b_w / 2) + (b_w / 4); i++)
                    {
                        for (int j = sty + (b_w / 2) - (b_w / 4); j < sty + (b_w / 2) + (b_w / 4); j++)
                        {
                            Color pix = bitmap.GetPixel(i, j);

                            // Check if the pixel color matches known colors for numbers.
                            for (int c = 0; c < num_col.Length; c++)
                            {
                                if (num_col[c].R == pix.R && num_col[c].G == pix.G && num_col[c].B == pix.B)
                                {
                                    col = c + 1;
                                    fnd = true;
                                    break;
                                }
                            }

                            // Check if the pixel color matches the flag color.
                            if (flag.R == pix.R && flag.G == pix.G && flag.B == pix.B)
                            {
                                col = -2;
                                fnd = true;
                                break;
                            }

                            if (fnd)
                            {
                                break;
                            }
                        }
                    }

                    // If the pixel color does not match known colors, it may be an empty cell or unknown.
                    if (!fnd)
                    {
                        Color refc = bitmap.GetPixel(stx + 3, sty + 3);
                        foreach (Color c in emp_col)
                        {
                            if (refc.R == c.R && refc.G == c.G && refc.B == c.B)
                            {
                                col = 0;
                                fnd = true;
                                break;
                            }
                        }

                        if (!fnd)
                        {
                            col = -1; // Unknown
                        }
                    }

                    grid[x, y] = col; // Update the grid with the identified cell state.
                }
            }
        }

        // Function to iterate through all cells on the grid and perform an action based on a provided function.
        static void iterate(Func<int, int, bool> func)
        {
            for (int x = 0; x < g_w; x++)
            {
                for (int y = 0; y < g_h; y++)
                {
                    if (func(x, y))
                    {
                        return;
                    }
                }
            }
        }

        // Function to set the mouse cursor to a specific grid cell.
        static void setmouse(int x, int y)
        {
            MouseOperations.SetCursorPosition(p_x + (x * b_w) + (b_w / 2), p_y + (y * b_w) + (b_w / 2));
        }

        // Function to simulate a right-click (flag) at a specific grid cell.
        static bool clickflag = false;
        static void flag_click(int x, int y)
        {
            if (clickflag)
            {
                setmouse(x, y);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.RightDown);
                Thread.Sleep(10);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.RightUp);
            }
        }

        // Function to simulate a left-click (open) at a specific grid cell.
        static void open_click(int x, int y)
        {
            setmouse(x, y);
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
            Thread.Sleep(10);
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
        }

        // List to store flagged cells.
        static List<(int, int)> flags = new List<(int, int)>();

        // Function to solve the Minesweeper grid.
        static bool solve()
        {
            bool changed = false;

            bool fll = false;
            iterate((x, y) => { if (grid[x, y] != -1) { fll = true; } return false; });

            if (!fll)
            {
                changed = true;

                open_click(0, 0);
                setmouse(-1, 0);
                return changed;
            }

            // Flag suitable squares based on nearby cell information.
            iterate((x, y) =>
            {
                int n = grid[x, y];
                if (n <= 0)
                {
                    return false;
                }

                List<(int, int)> empList = new List<(int, int)>();
                int flagc = 0;

                for (int i = Math.Max(x - 1, 0); i <= Math.Min(x + 1, g_w - 1); i++)
                {
                    for (int j = Math.Max(y - 1, 0); j <= Math.Min(y + 1, g_h - 1); j++)
                    {
                        int k = grid[i, j];
                        if (flags.Contains((i, j)))
                        {
                            flagc++;
                        }
                        else if (k == -1)
                        {
                            empList.Add((i, j));
                        }
                    }
                }

                if (flagc < n && empList.Count == (n - flagc))
                {
                    foreach (var elm in empList)
                    {
                        changed = true;

                        flags.Add(elm);
                        flag_click(elm.Item1, elm.Item2);
                        Console.WriteLine("Flag: {0}, {1}", elm.Item1, elm.Item2);

                        flagcount--;
                    }
                }

                return false;
            });

            // Open cells based on nearby cell information.
            List<(int, int)> clicked = new List<(int, int)>();
            iterate((x, y) =>
            {
                int n = grid[x, y];
                if (n <= 0)
                {
                    return false;
                }

                List<(int, int)> empList = new List<(int, int)>();
                int flagc = 0;

                for (int i = Math.Max(x - 1, 0); i <= Math.Min(x + 1, g_w - 1); i++)
                {
                    for (int j = Math.Max(y - 1, 0); j <= Math.Min(y + 1, g_h - 1); j++)
                    {
                        int k = grid[i, j];
                        if (flags.Contains((i, j)))
                        {
                            flagc++;
                        }
                        else if (k == -1)
                        {
                            empList.Add((i, j));
                        }
                    }
                }

                if (flagc == n && empList.Count > 0)
                {
                    foreach (var elm in empList)
                    {
                        if (clicked.Contains(elm))
                        {
                            continue;
                        }

                        changed = true;

                        clicked.Add(elm);
                        open_click(elm.Item1, elm.Item2);
                        Console.WriteLine("Open: {0}, {1}", elm.Item1, elm.Item2);
                    }
                }

                return false;
            });

            setmouse(-1, 0);

            return changed;
        }

        // Function to print the current state of the Minesweeper grid in the console.
        static void printgrid()
        {
            Console.Clear();
            char[] sym = { '.', 'P' };
            ConsoleColor[] clr =
            {
                ConsoleColor.White,
                ConsoleColor.Blue,
                ConsoleColor.Green,
                ConsoleColor.DarkRed,
                ConsoleColor.Magenta,
                ConsoleColor.Yellow,
                ConsoleColor.Cyan
            };
            for (int y = 0; y < g_h; y++)
            {
                for (int x = 0; x < g_w; x++)
                {
                    string op;
                    if (flags.Contains((x, y)))
                    {
                        op = sym[1].ToString();
                    }
                    else
                    {
                        op = (-1 * grid[x, y] > 0) ? sym[(-1 * grid[x, y]) - 1].ToString() : grid[x, y].ToString();
                    }

                    ConsoleColor ccol = ConsoleColor.Black;

                    if (op == "P")
                    {
                        ccol = ConsoleColor.Red;
                    }
                    else if (op == ".")
                    {
                        ccol = ConsoleColor.Gray;
                    }
                    else
                    {
                        ccol = clr[grid[x, y]];
                    }

                    Console.ForegroundColor = ccol;
                    Console.Write(op + " ");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            // Prompt the user to select the difficulty level.
            Console.Write("Enter difficulty [0: Easy, 1: Medium, 2: Hard]: ");
            if (Int32.TryParse(Console.ReadLine().Trim(), out int val))
            {
                if (val < 0 || 2 < val)
                {
                    Environment.Exit(-1);
                }

                // Set grid properties based on the selected difficulty level.
                p_x = profiles[val, 0];
                p_y = profiles[val, 1];
                g_w = profiles[val, 2];
                g_h = profiles[val, 3];
                b_w = profiles[val, 4];
                flagcount = profiles[val, 5];
                grid = new int[g_w, g_h];
            }
            else
            {
                Environment.Exit(-1);
            }

            Console.Clear();
            Console.WriteLine("Press enter to start.");
            Console.ReadLine();

            Console.WriteLine("Started.");

            update_grid();

            bool changed;
            do
            {
                printgrid();

                changed = solve();

                Thread.Sleep(anim_delay);

                update_grid();
            }
            while (changed && flagcount != 0);

            Console.WriteLine();
            Console.WriteLine("Program ended. Press enter to exit.");

            Console.ReadLine();
        }
    }
}
