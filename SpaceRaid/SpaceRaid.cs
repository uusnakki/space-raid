using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using Jypeli.Effects;
/// @author Joonas Uusnäkki
/// @version 27.11.2019
/// <summary>
/// Harjoitustyön koodi
/// </summary>
public class SpaceRaid : PhysicsGame
{
    private Image taustakuva = LoadImage("Pelitausta");
    private PhysicsObject avaruusalus;
    private AssaultRifle avaruusaluksenAse;
    private LaserGun laserVihu;
    private IntMeter AlienPisteLaskuri;
    private IntMeter HPpisteLaskuri;
    private EasyHighScore topLista = new EasyHighScore();
    
    private double vihuPaikka; //Alienien spawnauskoordinaatit
    private double vihuKoko; // Alienien koko


    /// <summary>
    /// Luodaan tausta, musiikki sekä alkuvalikko
    /// </summary>
    public override void Begin() 
    {
        Level.BackgroundColor = Color.Black;
        Camera.ZoomToLevel();
        GameObject tausta = new GameObject(Screen.Width, Screen.Height);
        tausta.Image = taustakuva;
        Add(tausta, -3);

        MediaPlayer.Play("taustamusiikki");
        MediaPlayer.IsRepeating = true;

        MultiSelectWindow alkuValikko = new MultiSelectWindow("Space Raid",
        "Aloita peli", "Lopeta");
        Add(alkuValikko);
        alkuValikko.AddItemHandler(0, AloitaPeli);
        alkuValikko.AddItemHandler(1, Exit);
    }


    /// <summary>
    /// Aloittaa pelin luomalla kentän ja asettamalla ohjaimet
    /// </summary>
    private void AloitaPeli()
    {
        LuoKentta();
        AsetaOhjaimet();
    }


    /// <summary>
    /// Pelaajan hävittyä ja High Score -näytön OK-nappia painamalla päästään takaisin valikkoon
    /// </summary>
    private void Valikkoon(Window sender)
    {
        ClearAll(); // Siivoaa kaiken pelistä pois
        Begin(); // Luo uudelleen alkuvalikon, musiikit ja taustan
    }


    /// <summary>
    ///  Aliohjelma kentälle
    /// </summary>
    private void LuoKentta()
    {
        avaruusalus = LuoAlus(Level.Left + 60.0, 0.0);

        Timer ajastin = new Timer();
        ajastin.Interval = 3.0; // Kuinka usein ajastin "laukeaa" sekunneissa
        ajastin.Timeout += delegate { LisaaVihuja(); }; // Aliohjelma, jota kutsutaan 2.5 sekunnin välein
        ajastin.Start(); // Ajastin pitää aina muistaa käynnistää

        Keyboard.Listen(Key.Space, ButtonState.Pressed, MuutaAjastinta, "Nopeuta", ajastin, -0.04); //Spacea painamalla nopeutetaan aliohjelman kutsumista

        LuoPistelaskuriAlienit();
        LuoPistelaskuriHP();
    }


    /// <summary>
    /// Nopeuttaa ajastinta, joka kutsuu aliohjelmaa tietyin aikavälein
    /// </summary>
    private void MuutaAjastinta(Timer muutettavaAjastin, double muutos)
    {
        if (muutettavaAjastin.Interval + muutos <= 0) return;
        muutettavaAjastin.Interval += muutos;
    }


    /// <summary>
    /// Luodaan alus, jota pelaaja ohjaa pelissä
    /// </summary>
    private PhysicsObject LuoAlus(double x, double y)
    {
        PhysicsObject alus = PhysicsObject.CreateStaticObject(320.0, 140.0);
        alus.X = x;
        alus.Y = y;
        alus.Image = LoadImage("alus");
        alus.IgnoresExplosions = true; 
        AddCollisionHandler(alus, AlusOsuu);
        Add(alus);
        avaruusaluksenAse = new AssaultRifle(1, 1);
        avaruusaluksenAse.X = -160.0;
        avaruusaluksenAse.Y = 0.0;
        avaruusaluksenAse.Image = LoadImage("tyhja");//Ammukset lähtevät suoraan aluksesta. Asetta ei ole siis "olemassa"
        avaruusaluksenAse.ProjectileCollision = AmmusOsuu;
        alus.Add(avaruusaluksenAse);
        return alus;
    }


    /// <summary>
    /// Aseen ampumisen parametrit, mm. nopeus,,ammukset.
    /// </summary>
    private void AmmuAseella(AssaultRifle avaruusaluksenAse)
    {
        avaruusaluksenAse.Shoot();
        avaruusaluksenAse.InfiniteAmmo = true;
        avaruusaluksenAse.Power.DefaultValue = 200;
        avaruusaluksenAse.AmmoIgnoresGravity = true;
        avaruusaluksenAse.FireRate = 2.0;
    }


    /// <summary>
    /// Alienin luonnin aliohjelma
    /// </summary>
    private PhysicsObject LuoVihu()
    {
        vihuKoko = RandomGen.NextDouble(130,300);
        PhysicsObject vihu = new PhysicsObject(vihuKoko, vihuKoko);
        vihuPaikka = RandomGen.NextDouble(Level.Bottom, Level.Top);
        vihu.Image = LoadImage("jees"); 
        vihu.X = Level.Right;
        vihu.Y = vihuPaikka;
        vihu.IgnoresExplosions = true;
        vihu.IgnoresCollisionWith(vihu);
        vihu.IgnoresGravity = true;
        vihu.CanRotate = false;
        vihu.CollisionIgnoreGroup = 1;
        vihu.Velocity = new Vector(-100, 0);
        
        laserVihu = new LaserGun(80, 30);
        laserVihu.Angle += Angle.FromDegrees(180);
        laserVihu.ProjectileCollision = LaserOsuu;
            
        Timer ajastin = new Timer();
        ajastin.Interval = 1.5; 
        ajastin.Timeout += delegate { AmmuVihu(laserVihu); }; // Aliohjelma, jota kutsutaan 1.5 sekunnin välein
        ajastin.Start(); 
        vihu.Add(laserVihu);
       
        if (vihu.IsDestroyed == true) laserVihu.Destroy();
    
        Add(vihu);
        return vihu;
    }


    /// <summary>
    /// Alienin aseen ampumisen parametrit, mm. nopeus,,ammukset.
    /// </summary>
    private void AmmuVihu(LaserGun laserVihu)
    {
        PhysicsObject ammus = laserVihu.Shoot();
        if (ammus != null)
        {
            ammus.Width = 35;
            ammus.Height = 4;
            ammus.Image = LoadImage("laser");
            ammus.CollisionIgnoreGroup = 1;
            if (laserVihu.IsDestroyed == true)
            {
                ammus.Destroy();
                laserVihu.AttackSound = null;
            }
        }
        laserVihu.FireRate = RandomGen.NextDouble(1, 3);
        laserVihu.Power.DefaultValue = 150;
        laserVihu.InfiniteAmmo = true;
    }


    /// <summary>
    /// Luo oikealle puolelle kenttää satunnaisesti alieneita nollasta neljään kappaletta
    /// </summary>
    private void LisaaVihuja()
    {
        int vihuMin = 0;
        int vihuMax = 5;  

        for (int i = RandomGen.NextInt(vihuMin,vihuMax); i < 5; i++)
        {
            LuoVihu();
        }
    }


    /// <summary>
    /// Aluksen osuessa alieniiin HP vähenee yhdellä ja alien poistuu kentältä
    /// </summary>
    private void AlusOsuu(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        HPpisteLaskuri.Value -= 1;
        kohde.Destroy();
        
        if (HPpisteLaskuri <= 0)
        {
            avaruusalus.Destroy();
            IsPaused = true;
            topLista.EnterAndShow(AlienPisteLaskuri.Value);
            topLista.HighScoreWindow.Closed += Valikkoon;
        }
    }


    /// <summary>
    /// Aluksen osuessa laseriin HP vähenee yhdellä
    /// </summary>
    private void LaserOsuu(PhysicsObject laser, PhysicsObject pelaaja) 
    {
        HPpisteLaskuri.Value -= 1;
        laser.Destroy();

        if (HPpisteLaskuri <= 0)
        {
            avaruusalus.Destroy();
            IsPaused = true;
            topLista.EnterAndShow(AlienPisteLaskuri.Value);
            topLista.HighScoreWindow.Closed += Valikkoon;
        }
    }


    /// <summary>
    /// Tapahtuma-aliohjelma aluksen ammuksen osuessa alieniin
    /// </summary>
    private void AmmusOsuu(PhysicsObject ammus, PhysicsObject vihu)
    {
        vihu.Destroy();
        ammus.Destroy();
        if (vihu.IsDestroyed == true)
        {
            AlienPisteLaskuri.Value += 1;
        }
        Explosion rajahdys = new Explosion(50);
        rajahdys.Position = vihu.Position;
        Add(rajahdys);
        ammus.Destroy();
    }


    /// <summary>
    /// Luodaan ohjainasetukset alukselle ja muille pelin ominaisuuksille
    /// </summary>
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, AsetaNopeus, "Liikuta alusta ylös", avaruusalus, new Vector(0, 700));
        Keyboard.Listen(Key.Up, ButtonState.Released, AsetaNopeus, null, avaruusalus, Vector.Zero);
        Keyboard.Listen(Key.Down, ButtonState.Down, AsetaNopeus, "Liikuta alusta alas", avaruusalus, new Vector(0, -700));
        Keyboard.Listen(Key.Down, ButtonState.Released, AsetaNopeus, null, avaruusalus, Vector.Zero);

        Keyboard.Listen(Key.Right, ButtonState.Down, AsetaNopeus, "Liikuta alusta oikealle", avaruusalus, new Vector(700, 0));
        Keyboard.Listen(Key.Right, ButtonState.Released, AsetaNopeus, null, avaruusalus, Vector.Zero);

        Keyboard.Listen(Key.Left, ButtonState.Down, AsetaNopeus, "Liikuta alusta vasemmalle", avaruusalus, new Vector(-700, 0));
        Keyboard.Listen(Key.Left, ButtonState.Released, AsetaNopeus, null, avaruusalus, Vector.Zero);

        Keyboard.Listen(Key.Space, ButtonState.Down, AmmuAseella, "Ammu", avaruusaluksenAse);


        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Aluksen liikkumisen nopeus ja pysähtyminen reinoille
    /// </summary>
    private void AsetaNopeus(PhysicsObject aalus, Vector nopeus) 
    {
        if ((nopeus.Y < 0) && (aalus.Bottom < Level.Bottom))
        {
            aalus.Velocity = Vector.Zero;
            return;
        }
        if ((nopeus.Y > 0) && (aalus.Top > Level.Top))
        {
            aalus.Velocity = Vector.Zero;
            return;
        }
        if ((nopeus.X < 0) && (aalus.Left < Level.Left))
        {
            aalus.Velocity = Vector.Zero;
            return;
        }
        if ((nopeus.X > 0) && (aalus.Right > Level.Right))
        {
            aalus.Velocity = Vector.Zero;
            return;
        }
        aalus.Velocity = nopeus;
    }


    /// <summary>
    /// Luo pistelaskurin, joka näyttää tuhoutuneiden alienien määrän
    /// </summary>
    private void LuoPistelaskuriAlienit() 
    {
        AlienPisteLaskuri = new IntMeter(0);

        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 70;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Title = "Alieneita tuhottu";
        pisteNaytto.BindTo(AlienPisteLaskuri);

        Add(pisteNaytto);
    }


    /// <summary>
    /// Luo pistelaskurin elämäpisteille, jonka arvo alussa on 10
    /// </summary>
    private void LuoPistelaskuriHP()
    {
        HPpisteLaskuri = new IntMeter(10);

        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 30;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Title = "HP";

        pisteNaytto.BindTo(HPpisteLaskuri);
        Add(pisteNaytto);
    }


}
