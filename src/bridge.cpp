#include <stdlib.h>

#include "bridge.h"

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Global variables:

static scidBaseT* db = NULL;       // current database.
static scidBaseT* clipbase = NULL; // clipbase database.
static scidBaseT* dbList = NULL;   // array of database slots.
static int currentBase = 0;
static Position* scratchPos = NULL;                         // temporary "scratch" position.
static Game* scratchGame = NULL;                            // "scratch" game for searches, etc.


// MAX_BASES is the maximum number of databases that can be open,
// including the clipbase database.
const int MAX_BASES = 9;
const int CLIPBASE_NUM = MAX_BASES - 1;


//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// recalcFlagCounts:
//    Updates all precomputed stats about the database: flag counts,
//    average rating, date range, etc.
void recalcFlagCounts(scidBaseT* basePtr)
{
    scidStatsT* stats = &(basePtr->stats);
    uint i;

    // Zero out all stats:
    for (i = 0; i < IDX_NUM_FLAGS; i++)
    {
        stats->flagCount[i] = 0;
    }
    stats->nRatings = 0;
    stats->sumRatings = 0;
    stats->minRating = 0;
    stats->maxRating = 0;
    stats->minDate = ZERO_DATE;
    stats->maxDate = ZERO_DATE;
    stats->nYears = 0;
    stats->sumYears = 0;
    for (i = 0; i < NUM_RESULT_TYPES; i++)
    {
        stats->nResults[i] = 0;
    }
    for (i = 0; i < 1; i++)
    {
        stats->ecoCount0[i].count = 0;
        stats->ecoCount0[i].results[RESULT_White] = 0;
        stats->ecoCount0[i].results[RESULT_Black] = 0;
        stats->ecoCount0[i].results[RESULT_Draw] = 0;
        stats->ecoCount0[i].results[RESULT_None] = 0;
    }
    for (i = 0; i < 5; i++)
    {
        stats->ecoCount1[i].count = 0;
        stats->ecoCount1[i].results[RESULT_White] = 0;
        stats->ecoCount1[i].results[RESULT_Black] = 0;
        stats->ecoCount1[i].results[RESULT_Draw] = 0;
        stats->ecoCount1[i].results[RESULT_None] = 0;
    }
    for (i = 0; i < 50; i++)
    {
        stats->ecoCount2[i].count = 0;
        stats->ecoCount2[i].results[RESULT_White] = 0;
        stats->ecoCount2[i].results[RESULT_Black] = 0;
        stats->ecoCount2[i].results[RESULT_Draw] = 0;
        stats->ecoCount2[i].results[RESULT_None] = 0;
    }
    for (i = 0; i < 500; i++)
    {
        stats->ecoCount3[i].count = 0;
        stats->ecoCount3[i].results[RESULT_White] = 0;
        stats->ecoCount3[i].results[RESULT_Black] = 0;
        stats->ecoCount3[i].results[RESULT_Draw] = 0;
        stats->ecoCount3[i].results[RESULT_None] = 0;
    }
    for (i = 0; i < 500 * 26; i++)
    {
        stats->ecoCount4[i].count = 0;
        stats->ecoCount4[i].results[RESULT_White] = 0;
        stats->ecoCount4[i].results[RESULT_Black] = 0;
        stats->ecoCount4[i].results[RESULT_Draw] = 0;
        stats->ecoCount4[i].results[RESULT_None] = 0;
    }
    // Read stats from index entry of each game:
    for (uint gnum = 0; gnum < basePtr->numGames; gnum++)
    {
        IndexEntry* ie = basePtr->idx->FetchEntry(gnum);
        stats->nResults[ie->GetResult()]++;
        eloT elo = ie->GetWhiteElo();
        if (elo > 0)
        {
            stats->nRatings++;
            stats->sumRatings += elo;
            if (stats->minRating == 0)
            {
                stats->minRating = elo;
            }
            if (elo < stats->minRating)
            {
                stats->minRating = elo;
            }
            if (elo > stats->maxRating)
            {
                stats->maxRating = elo;
            }
            basePtr->nb->AddElo(ie->GetWhite(), elo);
        }
        elo = ie->GetBlackElo();
        if (elo > 0)
        {
            stats->nRatings++;
            stats->sumRatings += elo;
            if (stats->minRating == 0)
            {
                stats->minRating = elo;
            }
            if (elo < stats->minRating)
            {
                stats->minRating = elo;
            }
            if (elo > stats->maxRating)
            {
                stats->maxRating = elo;
            }
            basePtr->nb->AddElo(ie->GetBlack(), elo);
        }
        dateT date = ie->GetDate();
        if (gnum == 0)
        {
            stats->maxDate = stats->minDate = date;
        }
        if (date_GetYear(date) > 0)
        {
            if (date < stats->minDate)
            {
                stats->minDate = date;
            }
            if (date > stats->maxDate)
            {
                stats->maxDate = date;
            }
            stats->nYears++;
            stats->sumYears += date_GetYear(date);
            basePtr->nb->AddDate(ie->GetWhite(), date);
            basePtr->nb->AddDate(ie->GetBlack(), date);
        }

        for (uint flag = 0; flag < IDX_NUM_FLAGS; flag++)
        {
            bool value = ie->GetFlag(1 << flag);
            if (value)
            {
                stats->flagCount[flag]++;
            }
        }

        ecoT eco = ie->GetEcoCode();
        ecoStringT ecoStr;
        eco_ToExtendedString(eco, ecoStr);
        uint length = strLength(ecoStr);
        resultT result = ie->GetResult();
        if (length >= 3)
        {
            uint code = 0;
            stats->ecoCount0[code].count++;
            stats->ecoCount0[code].results[result]++;
            code = ecoStr[0] - 'A';
            stats->ecoCount1[code].count++;
            stats->ecoCount1[code].results[result]++;
            code = (code * 10) + (ecoStr[1] - '0');
            stats->ecoCount2[code].count++;
            stats->ecoCount2[code].results[result]++;
            code = (code * 10) + (ecoStr[2] - '0');
            stats->ecoCount3[code].count++;
            stats->ecoCount3[code].results[result]++;
            if (length >= 4)
            {
                code = (code * 26) + (ecoStr[3] - 'a');
                stats->ecoCount4[code].count++;
                stats->ecoCount4[code].results[result]++;
            }
        }
    }
}




BOOL WINAPI DllMain(
    HINSTANCE hinstDLL,  // handle to DLL module
    DWORD fdwReason,     // reason for calling function
    LPVOID lpReserved)  // reserved
{
    // Perform actions based on the reason for calling.
    switch (fdwReason)
    {
    case DLL_PROCESS_ATTACH:
        // Initialize once for each new process.
        // Return FALSE to fail DLL load.
        // Initialise global Scid database variables:
        dbList = new scidBaseT[MAX_BASES];

        for (int base = 0; base < MAX_BASES; base++)
        {
            db = &(dbList[base]);
            db->idx = new Index;
            db->nb = new NameBase;
            db->game = new Game;
            for (int u = 0; u < UNDO_MAX; u++)
                db->undoGame[u] = NULL;
            db->undoMax = -1;
            db->undoIndex = -1;
            db->undoCurrent = -1;
            db->undoCurrentNotAvail = false;
            db->gameNumber = -1;
            db->gameAltered = false;
            db->gfile = new GFile;
            // TODO: Bases should be able to share common buffers!!!
            db->bbuf = new ByteBuffer;
            db->bbuf->SetBufferSize(BBUF_SIZE);
            db->tbuf = new TextBuffer;
            db->tbuf->SetBufferSize(TBUF_SIZE);
            strCopy(db->fileName, "");
            strCopy(db->realFileName, "");
            db->fileMode = FMODE_Both;
            db->inUse = false;
            db->filter = new Filter(0);
            db->dbFilter = db->filter;
            db->treeFilter = NULL;
            db->numGames = 0;
            db->memoryOnly = false;
            db->duplicates = NULL;
            db->idx->SetDescription("NOT OPEN");

            recalcFlagCounts(db);

            db->tree.moveCount = db->tree.totalCount = 0;
            db->treeCache = NULL;

            db->treeSearchTime = 0;
        }
        // Initialise the clipbase database:
        clipbase = &(dbList[CLIPBASE_NUM]);
        clipbase->gfile->CreateMemoryOnly();
        clipbase->idx->CreateMemoryOnly();
        clipbase->idx->SetType(2);
        clipbase->idx->SetDescription("Temporary database, not kept on disk.");
        clipbase->inUse = true;
        clipbase->memoryOnly = true;

        clipbase->treeCache = new TreeCache;
        clipbase->treeCache->SetCacheSize(SCID_TreeCacheSize);
        clipbase->backupCache = new TreeCache;
        clipbase->backupCache->SetCacheSize(SCID_BackupCacheSize);
        clipbase->backupCache->SetPolicy(TREECACHE_Oldest);

        currentBase = 0;
        scratchPos = new Position;
        scratchGame = new Game;
        db = &(dbList[currentBase]);

        break;

    case DLL_THREAD_ATTACH:
        // Do thread-specific initialization.
        break;

    case DLL_THREAD_DETACH:
        // Do thread-specific cleanup.
        break;

    case DLL_PROCESS_DETACH:
        // Perform any necessary cleanup.
        break;
    }
    return TRUE;  // Successful DLL_PROCESS_ATTACH.
}


// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// sc_base_autoload:
//   Sets or returns the autoload number of the database, which
//   is the game to load when opening the base.
int Base_autoload(bool getbase, uint basenum)
{
    if (getbase)
    {
        return db->idx->GetAutoLoad();
    }
    
    if (!db->inUse)
    {
        return -1;
    }
    if (db->fileMode == FMODE_ReadOnly)
    {
        return -2;
    }

    db->idx->SetAutoLoad(basenum);
    db->idx->WriteHeader();
    return 0;
}