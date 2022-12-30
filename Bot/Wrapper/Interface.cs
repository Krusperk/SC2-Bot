using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot {
    public interface IBot {
        IEnumerable<Action> OnFrame();
    }
}