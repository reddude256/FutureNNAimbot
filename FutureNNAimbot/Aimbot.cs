﻿using GameOverlay.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FutureNNAimbot
{
    public class Aimbot
    {
        private GameProcess gp;
        private gController gc;
        private NeuralNet nn;
        private Settings s;
        private DrawHelper dh;
        private int shooting = 0;
        private System.Drawing.Point coordinates;
        public bool Enabled = true;
        private string[] objects = null;

        public Aimbot(Settings settings, GameProcess gameProcess, gController gc, NeuralNet neuralNet, DrawHelper dh)
        {
            this.gp = gameProcess;
            this.gc = gc;
            this.nn = neuralNet;
            this.s = settings;
            this.dh = dh;
            this.objects = nn.TrainingNames;
        }

        public void Run()
        {
            if (Enabled)
            {
                coordinates = Cursor.Position;
                var bitmap = gc.ScreenCapture(true, coordinates);
                var items = nn.GetItems(bitmap);
                RenderItems(items);

                dh.DrawPlaying(coordinates, objects?[s.selectedObject], s, items);
            }
            else
            {
                dh.DrawDisabled();
            }
        }
        
        //static bool lastMDwnState = false;
        //static bool Firemode = false;
        //static long lastTick = DateTime.Now.Ticks;

        public void RenderItems(IEnumerable<Alturos.Yolo.Model.YoloItem> items)
        {
            shooting = 0;

            //var isMdwn = IsKeyPressed(Keys.LButton) || s.AutoShoot & IsKeyPressed(s.AimKey); // isKeyPressed(s.TriggerBotKey);
            //if (isMdwn || DateTime.Now.Ticks > lastTick + 20000000)
            //{               
            //    Firemode = isMdwn  || lastMDwnState;
            //    lastMDwnState = isMdwn;
            //    lastTick = DateTime.Now.Ticks;
            //}

            if (items.Count() > 0 && Util.IsKeyPressed(s.AimKey))
            {
                Shooting(ref items);
            }
        }

        void Shooting(ref IEnumerable<Alturos.Yolo.Model.YoloItem> items)
        {
            Rectangle enemyRectangle = Rectangle.Create(0,0,0,0);
            items = items.Where(x => x.Type == objects[s.selectedObject]);
            if (items.Count() == 0)
                return;
            if (s.Head)
            {
               var nearestEnemy = items.OrderBy(x => DistanceBetweenCross(x.X + Convert.ToInt32(x.Width / 2.9) + (x.Width / 3) / 2, x.Y + (x.Height / 7) / 2))
                    .First();
                enemyRectangle = Rectangle.Create(nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width / 2.9), 
                    nearestEnemy.Y, 
                    Convert.ToInt32(nearestEnemy.Width / 3), 
                    nearestEnemy.Height / 7 + (float)2 * shooting);
            }
            else // aim 2 body
            {
                var nearestEnemy = items.OrderBy(x => DistanceBetweenCross(x.X + Convert.ToInt32(x.Width / 6) + (x.Width / 1.5f / 2), x.Y + (x.Height / 6) + (x.Height / 3) / 2))
                    .First();
                enemyRectangle = Rectangle.Create(nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width / 6), 
                    nearestEnemy.Y + nearestEnemy.Height / 6 + (float)2 * shooting,
                    Convert.ToInt32(nearestEnemy.Width / 1.5f), 
                    nearestEnemy.Height / 3 + (float)2 * shooting);
            }

            if (s.SmoothAim <= 0)
            {
                VirtualMouse.Move(Convert.ToInt32(((enemyRectangle.Left - s.SizeX / 2) + (enemyRectangle.Width / 2))),
                    Convert.ToInt32((enemyRectangle.Top - s.SizeY / 2 + enemyRectangle.Height / (s.Head?3:7) + 1 * shooting)));

                if (s.SimpleRCS) shooting += 2;
            }
            else
            {
                if (s.SizeX / 2 < enemyRectangle.Left | s.SizeX / 2 > enemyRectangle.Right
                    | s.SizeY / 2 < enemyRectangle.Top | s.SizeY / 2 > enemyRectangle.Bottom)
                {
                    VirtualMouse.Move(Convert.ToInt32(((enemyRectangle.Left - s.SizeX / 2) + (enemyRectangle.Width / 2)) * s.SmoothAim), 
                        Convert.ToInt32((enemyRectangle.Top - s.SizeY / 2 + enemyRectangle.Height / 7 + 1 * shooting) * s.SmoothAim));
                }
                else
                {
                    if (s.SimpleRCS) shooting += 2;
                }
            }

            if (s.AutoShoot && !Util.IsKeyPressed(Keys.LButton))
                VirtualMouse.LeftClick();

            if (s.SimpleRCS)
                VirtualMouse.Move(0, shooting);

        }

        public void ReadKeys()
        {
            if (Util.IsKeyToggled(Keys.PageUp))
            {
                s.selectedObject = (s.selectedObject + 1) % nn.TrainingNames.Count();
            }

            if (Util.IsKeyToggled(Keys.Up))
            {
                s.SmoothAim = Math.Min(s.SmoothAim + 0.05f, 1);
            }

            if (Util.IsKeyToggled(Keys.Down))
            {
                s.SmoothAim = Math.Max(s.SmoothAim - 0.05f, 0);
            }

            if (Util.IsKeyToggled(Keys.Delete))
            {
                s.Head = !s.Head;
            }

            if (Util.IsKeyToggled(Keys.Home))
            {
                shooting = 0;
                s.SimpleRCS = !s.SimpleRCS;
            }

            if (Util.IsKeyToggled(Keys.End))
            {
                s.AutoShoot = !s.AutoShoot;
            }

            if (Util.IsKeyToggled(Keys.PageDown))
            {
                s.selectedObject = (s.selectedObject - 1 + nn.TrainingNames.Count()) % nn.TrainingNames.Count();
            }

        }

        public float DistanceBetweenCross(float X, float Y)
        {
            float ydist = (Y - s.SizeY / 2);
            float xdist = (X - s.SizeX / 2);
            return (float)Math.Sqrt(Math.Pow(ydist, 2) + Math.Pow(xdist, 2));
        }
        
    }
}
