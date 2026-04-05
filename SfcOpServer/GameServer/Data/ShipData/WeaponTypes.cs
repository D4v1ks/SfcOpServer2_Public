#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum WeaponTypes
    {
        None = -1,

        Photon, PhotonFighter, HeavyPhoton,
        Disruptor1, Disruptor2, Disruptor3, Disruptor4, DisruptorFighter, HeavyDisruptor,
        Fusion, FusionFighter,
        Hellbore, HellboreFighter,
        PlasmaR, PlasmaS, PlasmaG, PlasmaF, PlasmaI, PlasmaD, PlasmaX, PlasmaE,
        ESG, ESGLance,
        PPD,
        Mauler,
        PhaserA, PhaserB,
        TRBeamLight, TRBeamHeavy,
        Unknown, // ShuttleWeapon or FighterSystem
        DroneRackA, DroneRackB, DroneRackC, DroneRackD, DroneRackE, DroneRackF, DroneRackG, DroneRackH, DroneFighter1, DroneFighter4, DroneRackMIRV,
        Optional, OptionalW,
        Phaser1, Phaser2, Phaser3, PhaserG, PhaserG2, Phaser4, PhaserX,
        ADD6, ADD12, ADD30,

        Total
    }
}
