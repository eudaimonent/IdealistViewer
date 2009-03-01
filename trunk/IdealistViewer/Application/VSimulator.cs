using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace IdealistViewer
{
    public class VSimulator
    {
        public VSimulator(Simulator simulator)
        {
            handle = simulator.Handle;
            waterHeight = simulator.WaterHeight;
        }

        public VSimulator()
        {
        }

        private UUID id=UUID.Zero;
        public UUID Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }


        private ulong handle=1;
        public ulong Handle 
        {
            get
            {
                return handle;
            }
            set
            {
                handle = value;
            }
        }

        private float waterHeight;
        public float WaterHeight
        {
            get
            {
                return waterHeight;
            }
            set
            {
                waterHeight = value;
            }
        }
        
    }
}
