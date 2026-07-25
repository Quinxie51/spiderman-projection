const fs = require('fs');
const path = require('path');

const ENGINE = '/Users/sle2/.codex/plugins/cache/ls-extensions/ls-clad/local/skills/build-sfx/tools';
const audio = require(ENGINE);
const p = audio.sfx_presets;

const PROJECT_ASSETS_SFX = '/Users/sle2/Desktop/Specs Base Template/Assets/GeneratedSFX';
fs.mkdirSync(PROJECT_ASSETS_SFX, { recursive: true });

function asStereo(buffer) {
    return buffer && buffer.left ? buffer : audio.stereoFromMono(buffer);
}

function mixWeighted(layers) {
    const stereos = layers.map(({ buffer, gain = 1 }) => {
        const stereo = asStereo(buffer);
        const left = Float32Array.from(stereo.left, (v) => v * gain);
        const right = Float32Array.from(stereo.right, (v) => v * gain);
        return { left, right };
    });
    return audio.mixStereo(stereos);
}

function cloth(duration = 0.24, center = 2100) {
    return audio.granular.grainCloud({
        source: 'pink',
        duration,
        grainSizeMs: 16,
        density: 150,
        ampJitter: 0.65,
        pitchSpread: 3,
        panSpread: 0.28,
        filter: { type: 'bp', freq: center, Q: 1.2 },
    });
}

function webSnap(pitch = 0) {
    const snap = p.laser({ size: 0.22 + pitch * 0.01 });
    const air = p.swish({ duration: 0.16, size: 0.42, direction: 'up' });
    return mixWeighted([
        { buffer: snap, gain: 0.42 },
        { buffer: air, gain: 0.78 },
    ]);
}

function tensionCreak() {
    const pluck = audio.synth_voices.pluckString(48, 0.22, 76, 120);
    const rub = cloth(0.36, 1250);
    return audio.mix_bus.applyFx(
        mixWeighted([
            { buffer: pluck, gain: 0.55 },
            { buffer: rub, gain: 0.42 },
        ]),
        { hpf: 120, lpf: 3200, reverb: 'smallRoom', gain: 0.85 }
    );
}

function spiderSense() {
    const chime = p.uiNotify();
    const shimmer = p.sparkle({ duration: 0.65 });
    return audio.mix_bus.applyFx(
        mixWeighted([
            { buffer: chime, gain: 0.68 },
            { buffer: shimmer, gain: 0.34 },
        ]),
        { hpf: 260, width: 1.25, gain: 0.82 }
    );
}

function write(name, buffer, opts = {}) {
    const result = asStereo(buffer);
    audio.fadeOut(result.left, opts.fadeOut || 0.008);
    audio.fadeOut(result.right, opts.fadeOut || 0.008);
    audio.mix_bus.masterChain(result, {
        normalize: 'peak',
        peakCeiling: opts.peakCeiling || 0.92,
        width: opts.width || 1.0,
    });
    const output = path.join(PROJECT_ASSETS_SFX, `${name}.wav`);
    audio.WavBuilder.write(result, output);
    console.log(JSON.stringify({ kind: 'sfx', name, output, samples: result.left.length }));
}

// Movement and contact variations.
for (let i = 1; i <= 4; i++) {
    write(`sfx_footstep_wood_0${i}`, p.footstep({ surface: 'wood' }));
}
write('sfx_jump', mixWeighted([
    { buffer: p.jump({ pitch: -2, retro: false }), gain: 0.48 },
    { buffer: p.swish({ duration: 0.22, size: 0.55, direction: 'up' }), gain: 0.7 },
]));
write('sfx_land_soft', mixWeighted([
    { buffer: p.impact({ material: 'soft', size: 0.45 }), gain: 0.62 },
    { buffer: p.footstep({ surface: 'wood' }), gain: 0.56 },
]));
write('sfx_land_hard', mixWeighted([
    { buffer: p.impact({ material: 'wood', size: 0.92 }), gain: 0.84 },
    { buffer: cloth(0.28, 1750), gain: 0.3 },
]));
write('sfx_roll', mixWeighted([
    { buffer: p.swish({ duration: 0.42, size: 0.75, direction: 'by' }), gain: 0.68 },
    { buffer: cloth(0.52, 1450), gain: 0.58 },
]));
write('sfx_skid', mixWeighted([
    { buffer: p.swish({ duration: 0.38, size: 0.5, direction: 'down' }), gain: 0.58 },
    { buffer: audio.granular.grainCloud({
        source: 'white', duration: 0.42, grainSizeMs: 8, density: 210,
        ampJitter: 0.7, panSpread: 0.2, filter: { type: 'bp', freq: 2900, Q: 1.1 },
    }), gain: 0.42 },
]));
write('sfx_wall_crawl', cloth(0.48, 1900));
write('sfx_ledge_grab', mixWeighted([
    { buffer: p.impact({ material: 'soft', size: 0.32 }), gain: 0.6 },
    { buffer: cloth(0.32, 1650), gain: 0.55 },
]));

// Web and swing system.
write('sfx_web_shoot_01', webSnap(-1));
write('sfx_web_shoot_02', webSnap(0));
write('sfx_web_shoot_03', webSnap(1));
write('sfx_web_attach_wall', p.impact({ material: 'stone', size: 0.27 }));
write('sfx_web_attach_wood', p.impact({ material: 'wood', size: 0.24 }));
write('sfx_web_attach_metal', p.impact({ material: 'metal', size: 0.2 }));
write('sfx_web_tension', tensionCreak());
write('sfx_web_release', p.swish({ duration: 0.17, size: 0.38, direction: 'down' }));
write('sfx_swing_whoosh_01', p.whoosh({ duration: 0.64, size: 0.72, direction: 'by' }), { width: 1.15 });
write('sfx_swing_whoosh_02', p.whoosh({ duration: 0.72, size: 0.82, direction: 'up' }), { width: 1.18 });
write('sfx_swing_whoosh_03', p.whoosh({ duration: 0.58, size: 0.64, direction: 'down' }), { width: 1.12 });

// Feedback, UI, and state cues.
write('sfx_hurt', p.hurt({ pitch: -3, retro: false }));
write('sfx_spider_sense', spiderSense(), { width: 1.2 });
write('sfx_checkpoint', p.uiSuccess(), { width: 1.12 });
write('sfx_ui_move', p.uiHover({ pitch: -1 }));
write('sfx_ui_confirm', p.uiClick({ character: 'sharp', pitch: 1 }));
write('sfx_ui_back', p.uiToggle({ on: false, pitch: -2 }));
write('sfx_pause_open', p.uiToggle({ on: true, pitch: 0 }));
write('sfx_pause_close', p.uiToggle({ on: false, pitch: 0 }));
