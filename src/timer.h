//////////////////////////////////////////////////////////////////////
//
//  FILE:       timer.h
//              Millisecond resolution timer class
//
//  Part of:    Scid (Shane's Chess Information Database)
//  Version:    1.7
//
//  Notice:     Copyright (c) 1999  Shane Hudson.  All rights reserved.
//
//  Author:     Shane Hudson (sgh@users.sourceforge.net)
//
//////////////////////////////////////////////////////////////////////

#ifndef SCID_TIMER_H
#define SCID_TIMER_H

//////////////////////////////////////////////////////////////////////
// Timer::MilliSecs() returns the number of milliseconds since the
// timer was constructed or its Reset() method was last called.
// It uses gettimeofday() in Unix, or ftime() in Windows.


#  include <sys/timeb.h>


struct msecTimerT {
    long seconds;
    long milliseconds;
};


inline static void 
setTimer (msecTimerT *t)
{
    // Use ftime() call in Windows:
    struct timeb tb;
    ftime (&tb);
    t->seconds = tb.time;
    t->milliseconds = tb.millitm;
}


class Timer {

  private:

    msecTimerT StartTime;

  public:
  
    Timer() { Reset (); }
    ~Timer() {}
    void Reset() { setTimer (&StartTime); }

    int MilliSecs (void) {
        msecTimerT nowTime;
        setTimer (&nowTime);
        return 1000 * (nowTime.seconds - StartTime.seconds) +
                    (nowTime.milliseconds - StartTime.milliseconds);
    }

    int CentiSecs (void) {
        msecTimerT nowTime;
        setTimer (&nowTime);
        return 100 * (nowTime.seconds - StartTime.seconds) +
                    (nowTime.milliseconds - StartTime.milliseconds) / 10;
    }

};

#endif

//////////////////////////////////////////////////////////////////////
//  EOF: timer.h
//////////////////////////////////////////////////////////////////////
