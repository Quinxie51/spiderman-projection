const fs = require('fs');
const path = require('path');

const ENGINE = '/Users/sle2/.codex/plugins/cache/ls-extensions/ls-clad/local/skills/build-music/tools';
const m = require(ENGINE);
const PROJECT_ASSETS_SFX = '/Users/sle2/Desktop/Specs Base Template/Assets/GeneratedSFX';
fs.mkdirSync(PROJECT_ASSETS_SFX, { recursive: true });

function scaleRootAbove(meta, octaves = 1) {
    const match = meta.key.match(/^([A-Ga-g][#b]?)(-?\d+)$/);
    if (!match) return meta.key;
    return match[1] + (parseInt(match[2], 10) + octaves);
}

function write(name, out, meta) {
    const output = path.join(PROJECT_ASSETS_SFX, `${name}.wav`);
    m.WavBuilder.write(out, output);
    console.log(JSON.stringify({ kind: 'music', name, output, meta }));
}

function buildActionTheme() {
    const genre = 'synthwave';
    const bars = 16;
    const bpm = m.suggestTempo(genre);
    const { chords, meta } = m.composeChords({ genre, voice: 'pad' });
    const form = m.arrangement.arrangement16();

    const padEvents = m.mask(
        m.chordEvents(chords, { voice: 'pad', bars, velocity: 54 }),
        form.full,
        bars,
        4
    );
    const brassEvents = m.mask(
        m.chordEvents(chords, { voice: 'analogBrass', bars, velocity: 70, stagger: 0.05 }),
        form.a,
        bars,
        4
    );
    const bassEvents = m.mask(
        m.composeBass({ chords, genre, style: 'offbeat-8ths', bars, variation: 0.48 }),
        form.a,
        bars,
        4
    );
    const melodyEvents = m.mask(
        m.composeMelody({
            chords,
            bars,
            notesPerBar: 8,
            octaveShift: 1,
            contour: 'arch',
            scale: meta.scale,
            scaleRoot: scaleRootAbove(meta, 1),
            restProbability: 0.25,
        }),
        form.b,
        bars,
        4
    );
    const expandedChords = Array.from({ length: bars }, (_, i) => chords[i % chords.length]);
    const arpEvents = m.mask(
        m.composeArpeggio({
            chords: expandedChords,
            style: 'up-down',
            density: 8,
            velocity: 62,
            octave: 1,
        }),
        form.a,
        bars,
        4
    );
    const drums = m.composeDrums({ genre, bars, energy: 0.78, fills: true, embellish: 0.65 });

    const tracks = [
        m.track('action_pad', 'pad', padEvents, {
            fx: {
                hpf: 210,
                lpf: 3300,
                chorus: { voices: 3, mix: 0.28 },
                reverb: 'largeHall',
                gain: 0.22,
                width: 1.18,
            },
        }),
        m.track('action_brass', 'analogBrass', brassEvents, {
            fx: { hpf: 260, lpf: 4200, reverb: 'mediumRoom', gain: 0.23 },
        }),
        m.track('action_bass', 'synthBass', bassEvents, {
            fx: { hpf: 48, lpf: 1050, distort: 1.4, gain: 0.56 },
            humanize: { timeJitter: 0.004, velJitter: 0.08 },
        }),
        m.track('action_arp', 'pluckSynth', arpEvents, {
            fx: { hpf: 520, lpf: 5200, delay: { time: 0.16, feedback: 0.22, wet: 0.18 }, gain: 0.24, pan: -0.08 },
        }),
        m.track('action_lead', 'synthLead', melodyEvents, {
            fx: { hpf: 480, lpf: 5800, reverb: 'mediumRoom', gain: 0.31, pan: 0.05 },
        }),
        ...drums,
    ];

    const out = m.render(tracks, {
        bpm,
        master: { normalize: 'peak', glue: true, width: 1.12 },
    });
    return { out, meta: { bpm, bars, genre, harmony: meta, drums: drums.meta, loopRecommendation: 'crossfade 0.35s' } };
}

function buildProjectionIdle() {
    const genre = 'ambient';
    const bars = 8;
    const bpm = m.suggestTempo(genre);
    const { chords, meta } = m.composeChords({ genre, voice: 'pad' });
    const form = m.arrangement.arrangement8();

    const padEvents = m.mask(
        m.chordEvents(chords, { voice: 'pad', bars, barsPerChord: 2, velocity: 52 }),
        form.full,
        bars,
        4
    );
    const pianoEvents = m.mask(
        m.chordEvents(chords, { voice: 'piano', bars, barsPerChord: 2, velocity: 60, stagger: 0.16 }),
        form.a,
        bars,
        4
    );
    const expandedChords = Array.from({ length: Math.ceil(bars / 2) }, (_, i) => chords[i % chords.length]);
    const arpEvents = m.mask(
        m.composeArpeggio({
            chords: expandedChords,
            barsPerChord: 2,
            style: 'alberti',
            density: 4,
            velocity: 50,
            octave: 1,
        }),
        form.b,
        bars,
        4
    );

    const tracks = [
        m.track('idle_pad', 'pad', padEvents, {
            fx: {
                hpf: 170,
                lpf: 2700,
                chorus: { voices: 3, mix: 0.34 },
                reverb: 'cathedral',
                gain: 0.29,
                width: 1.24,
            },
        }),
        m.track('idle_piano', 'piano', pianoEvents, {
            fx: { hpf: 260, lpf: 3900, reverb: 'largeHall', gain: 0.27, pan: -0.08 },
        }),
        m.track('idle_arp', 'vibraphone', arpEvents, {
            fx: { hpf: 430, lpf: 4700, reverb: 'largeHall', gain: 0.21, pan: 0.12 },
            humanize: { timeJitter: 0.012, velJitter: 0.14 },
        }),
    ];

    const out = m.render(tracks, {
        bpm,
        master: { normalize: 'peak', glue: true, width: 1.2 },
    });
    return { out, meta: { bpm, bars, genre, harmony: meta, loopRecommendation: 'crossfade 1.0s' } };
}

function buildVictoryStinger() {
    const genre = 'pop';
    const bars = 4;
    const bpm = m.suggestTempo(genre);
    const { chords, meta } = m.composeChords({ genre, voice: 'piano', scale: 'major' });
    const comp = m.chordEvents(chords, { voice: 'piano', bars, velocity: 78, stagger: 0.08 });
    const bass = m.composeBass({ chords, genre, style: 'root-fifth', bars, variation: 0.25 });
    const melody = m.composeMelody({
        chords,
        bars,
        notesPerBar: 4,
        octaveShift: 2,
        contour: 'rising',
        scale: meta.scale,
        scaleRoot: scaleRootAbove(meta, 1),
        restProbability: 0.08,
        velocityStrong: 102,
        velocityWeak: 78,
    });
    const drums = m.composeDrums({ genre, bars, energy: 0.66, fills: true, embellish: 0.55 });

    const tracks = [
        m.track('victory_piano', 'piano', comp, {
            fx: { hpf: 220, lpf: 5000, reverb: 'mediumRoom', gain: 0.43 },
        }),
        m.track('victory_bass', 'subBass', bass, { fx: { hpf: 42, lpf: 240, gain: 0.48 } }),
        m.track('victory_lead', 'marimba', melody, {
            fx: { hpf: 480, lpf: 6000, reverb: 'mediumRoom', gain: 0.38 },
        }),
        ...drums,
    ];
    const out = m.render(tracks, {
        bpm,
        master: { normalize: 'peak', glue: true, width: 1.1 },
    });
    return { out, meta: { bpm, bars, genre, harmony: meta, drums: drums.meta, loopRecommendation: 'do not loop' } };
}

const action = buildActionTheme();
write('music_wallcrawler_action', action.out, action.meta);

const idle = buildProjectionIdle();
write('music_projection_idle', idle.out, idle.meta);

const victory = buildVictoryStinger();
write('music_victory_stinger', victory.out, victory.meta);
