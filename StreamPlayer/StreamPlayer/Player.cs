using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NovaDll;
using TagLib;

namespace StreamPlayer
{

    public class Player : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        private static String MUSIC_PATH = Path.Combine(Environment.CurrentDirectory, "Musics");

        int imageHeight = 300;
        int imageWidth = 500;

        Nova_Particle[] particles = new Nova_Particle[256];
        List<Nova_Particle> emiters = new List<Nova_Particle>();

        enum Stages { show, wait, hide }
        Stages stages = new Stages();

        Boolean isShow = true;

        List<Song> songs = new List<Song>();
        List<Texture2D> arts = new List<Texture2D>();
        int index = 0;

        public Player()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Nova_Functions.SetGraphics(graphics);
            Nova_Functions.SetGame(this);
            Nova_Functions.ConfigureGraphics(true, true, false, SurfaceFormat.Color, DepthFormat.None);
            Nova_Functions.ChangeResolution(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, imageHeight, false);
            GetForm().Location = new System.Drawing.Point(0, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - imageHeight);
            IsMouseVisible = true;         
        }

        protected override void Initialize()
        {
            base.Initialize();
            GetForm().TransparencyKey = System.Drawing.Color.Black;
            GetForm().BackColor = System.Drawing.Color.Black;
            GetForm().Opacity = 0f;
            GetForm().TopMost = true;
            GetForm().FormBorderStyle = FormBorderStyle.None;
        }

        public Form GetForm()
        {
            return Nova_Functions.GetWindowsFormFrom(Window.Handle);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Nova_Importer.SetContent(Content);
            Nova_Functions.SetViewport(GraphicsDevice);

            int resourceNumber = 0;

            foreach (FileInfo file in new DirectoryInfo(MUSIC_PATH).GetFiles("*.mp3", SearchOption.AllDirectories))
            {
                songs.Add((Song)Nova_Importer.LoadExternalAndReturn(Path.Combine(Environment.CurrentDirectory, "Content", "Musics"), file, "MUSIC" + resourceNumber.ToString(), Nova_Importer.ImportType.Music));
                TagLib.File fileB = TagLib.File.Create(file.FullName);
                if (fileB.Tag.Pictures.Any())
                {
                    byte[] bin = fileB.Tag.Pictures[0].Data.Data;
                    String imageFileDest = Path.Combine(Path.GetTempPath(), Nova_Functions.GetFileName(file) + "IMAGE" + ".jpg");
                    Image.FromStream(new MemoryStream(bin)).Save(imageFileDest, System.Drawing.Imaging.ImageFormat.Jpeg);
                    arts.Add((Texture2D)Nova_Importer.LoadExternalAndReturn(Path.Combine(Environment.CurrentDirectory, "Content", "Images"), new FileInfo(imageFileDest), Nova_Functions.GetFileName(file) + "IMAGE", Nova_Importer.ImportType.Texture));
                }
                else
                {
                    arts.Add(null);
                }
                
                resourceNumber++;
            }
            Nova_Importer.LoadResource("font", "font");
            Nova_Importer.LoadResource("default", "default");
            Nova_Importer.LoadResource("bar", "bar");

            if (arts[arts.Count-1] == null)
            {
                arts[arts.Count - 1] = Nova_DataBase.GetTexture("default");
            }

            LoadParticles();


            Nova_Audio.playMusicViaSound(songs[0]);
        }

        private void LoadParticles()
        {
            for (int i = 0; i < 256; i++)
            {
                Nova_Particle p = new Nova_Particle();
                p.SetTexture(Nova_DataBase.GetTexture("bar"), SpriteEffects.None, Microsoft.Xna.Framework.Color.Purple);
                p.Position = new Vector2(imageWidth + 50 + (i * 5), 160);
                particles[i] = p;
            }
        }

        protected override void UnloadContent()
        {
            Nova_DataBase.CleanResources();
        }

        public void Controls()
        {
            if (GetAsyncKeyState(System.Windows.Forms.Keys.MediaPlayPause) == -0x8000)
            {
                PlayPause();
            }
            if (GetAsyncKeyState(System.Windows.Forms.Keys.MediaNextTrack) == -0x8000)
            {
                Next();
            }
            if (GetAsyncKeyState(System.Windows.Forms.Keys.MediaPreviousTrack) == -0x8000)
            {
                Prev();
            }
            if (MediaPlayer.State == MediaState.Stopped)
            {
                Next();
            }
  
        }

        int timeShowed = 0;

        public void ControlOpacity(GameTime gameTime)
        {
            if (isShow)
            {
                if (stages == Stages.show)
                {
                    if (GetForm().Opacity < 0.9f)
                        GetForm().Opacity += 0.01f;
                    else
                    {
                        stages = Stages.wait;
                    }
                }
                else if (stages == Stages.wait)
                {
                    timeShowed += gameTime.ElapsedGameTime.Milliseconds;
                    if (timeShowed > 4000)
                    {
                        timeShowed = 0;
                        stages = Stages.hide;
                    }
                }
                else
                {
                    if (GetForm().Opacity > 0)
                        GetForm().Opacity -= 0.005f;
                    else
                    {
                        stages = Stages.show; 
                        isShow = false;
                        PerformNormalState();
                    }
                }
            }
        }

        public void PlayPause()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
            else if (MediaPlayer.State == MediaState.Paused || MediaPlayer.State == MediaState.Stopped)
            {
                MediaPlayer.Resume();
            }
        }

        private void PerformShowOpacity()
        {
            GetForm().Opacity = 0;
            isShow = true;
            timeShowed = 0;
            stages = Stages.show;
            Nova_Functions.ChangeResolution(Nova_Functions.View.Width, 300, true);
            for (int i = 0; i < 256; i++)
            {
                particles[i].Position = new Vector2(imageWidth + 50 + (i * 5), 160);
            }
        }

        private void PerformNormalState()
        {
            GetForm().Opacity = 0.5f;
            timeShowed = 0;
            stages = Stages.show;
            Nova_Functions.ChangeResolution(Nova_Functions.View.Width, 150, true);
            for (int i = 0; i < 256; i++)
            {
                particles[i].Position = new Vector2((i * 7.4f), 160);
            }
        }

        public void Next()
        {
            PerformShowOpacity();
            if (index < songs.Count - 1)
            {
                index++;
            }
            MediaPlayer.Play(songs[index]);
        }

        public void Prev()
        {
            PerformShowOpacity();
            if (index > 0)
            {
                index--;
            }
            MediaPlayer.Play(songs[index]);
        }

        public void UpdatePlayer()
        {
            Nova_Audio.Update();
            sumPower = 0;
            for (int i = 0; i < 256; i++)
            {
                particles[i].inflateSizeHeight = (int)(Nova_Audio.GetCurrentFrequencies()[i] * 100);
                particles[i].Position = new Vector2(particles[i].Position.X - particles[i].GetCurrentTexture().Width / 2, isShow ? 160 : 75);
                if (i < 30)
                    sumPower += Nova_Audio.GetCurrentFrequencies()[i];
            }
        }

        float sumPower = 0;

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Controls();
            ControlOpacity(gameTime);
            foreach(Nova_Particle p in particles)
            {
                p.Update(gameTime, Matrix.CreateTranslation(0, 0, 0));
            }
            if (!isShow)
            {
                Nova_Particle p = new Nova_Particle();
                p.SetTexture(Nova_DataBase.GetTexture("bar"), SpriteEffects.None, Microsoft.Xna.Framework.Color.Blue);
                p.Position = new Vector2(0, 75);
                p.LifeTime = 1500;
                p.InitialLifeTime = 1500;
                p.SetFadeOut(1500);
                p.IsAllColorsUntilDie = true;
                p.SetDirectionSpeed(new Vector2(20, 0));
                p.inflateSizeHeight = (int)(sumPower * 3);
                p.SetInternalRotation(0, 1, Nova_Functions.GetCenterOf(p.GetCurrentTexture()), Nova_Particle.RotationDirectionEnum.clockwise);
                p.inflateSizeWidth = 10;
                emiters.Add(p);

                Nova_Particle.DoUpdateParticles(emiters, gameTime, Matrix.CreateTranslation(0,0,0));
                
            }
            UpdatePlayer();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
            GetForm().TopMost = true;
            GetForm().Show();
            GetForm().BringToFront();

            if (isShow)
            {
                GetForm().Location = new System.Drawing.Point(0, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - imageHeight);

                spriteBatch.Begin();
                Vector2 centerImagePosition = new Vector2(0, (isShow ? 300 : 150) - imageHeight);
                spriteBatch.Draw(arts[index] == null ? Nova_DataBase.GetTexture("default") : arts[index], new Microsoft.Xna.Framework.Rectangle((int)centerImagePosition.X, (int)centerImagePosition.Y, imageWidth, imageHeight), Microsoft.Xna.Framework.Color.White);
                Nova_Functions.DrawBorderString(spriteBatch, Nova_DataBase.GetFont("font"), songs[index].Name.Substring(songs[index].Name.IndexOf("Musics") + 1 + "Musics".Length), new Vector2(550, 10), Microsoft.Xna.Framework.Color.Red, Microsoft.Xna.Framework.Color.Blue);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                foreach (Nova_Particle p in particles)
                {
                    p.Draw(spriteBatch);
                }
                spriteBatch.End();
            }
            else
            {

                GetForm().Opacity = 0.5f;
                GetForm().Location = new System.Drawing.Point(0, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - imageHeight / 2);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Nova_Particle.DoDrawParticles(emiters, spriteBatch);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
