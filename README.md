# msbot
Google minesweeper bot. Algorithm is not complete, doesn't work ~20% of the time at some point.

Screen coordinates are measured for 1920x1080. You can change it from [``Program.cs:41 (int[,] profiles)``](msbot/Program.cs#L41). Format: 
- p_x (start pos x), 
- p_y (start pos y), 
- g_w (grid width, in squares), 
- g_h (grid height, in squares), 
- b_w (square width), 
- flagcount

You can also change the animation delay (anim_delay, currently 620 ms), I didn't exactly measure it.
 
[YouTube Video](https://www.youtube.com/watch?v=tFv1AFjhJvE)

Feel free to contribute. I won't modify this repo again i think.

## Download
You can download the release build from [here](/releases/latest).

## License
This project is licensed under [MIT License](LICENSE).
