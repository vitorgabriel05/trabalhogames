"""Compose a quiet C-major continuation and short menu sounds. No external samples."""
from pathlib import Path
import wave
import numpy as np

RATE = 32000
DEST = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
DEST.mkdir(parents=True, exist_ok=True)

def save(name, samples):
    with wave.open(str(DEST / (name + '.wav')), 'wb') as out:
        out.setnchannels(2)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes((np.clip(samples, -1, 1) * 32767).astype('<i2').tobytes())

def frequency(note):
    return 440 * 2 ** ((note - 69) / 12)

length = 96
music = np.zeros((length * RATE, 2), dtype=np.float64)

def note(at, midi, duration, level, pan, attack=.4):
    t = np.arange(int(duration * RATE)) / RATE
    f = frequency(midi)
    # Rounded chiptune timbre: predominantly sine with soft upper harmonics.
    tone = np.sin(2*np.pi*f*t) + .13*np.sin(2*np.pi*f*2*t) + .035*np.sin(2*np.pi*f*3*t)
    envelope = np.minimum(t / attack, 1) * np.minimum((duration - t) / 1.4, 1)
    envelope *= .85 + .15*np.cos(2*np.pi*t / duration)
    signal = tone * np.maximum(envelope, 0) * level
    # Circular placement includes reverberation tails from the previous loop at its start.
    for delay, gain in [(0, 1), (.31, .12), (.67, .06), (1.07, .025)]:
        idx = (int((at + delay)*RATE) + np.arange(len(t))) % len(music)
        music[idx, 0] += signal * gain * np.sqrt((1-pan)/2)
        music[idx, 1] += signal * gain * np.sqrt((1+pan)/2)

chords = [[48,55,60,64], [45,52,55,60], [41,48,57,60], [43,50,55,62],
          [48,55,59,64], [45,52,60,64], [41,48,55,60], [43,50,57,62],
          [48,55,60,64], [52,55,59,62], [45,52,55,60], [41,48,57,64],
          [43,50,55,60], [43,50,55,62], [48,55,60,64], [48,55,60,64]]
for bar, chord in enumerate(chords):
    for j, pitch in enumerate(chord):
        note(bar*6, pitch, 8, .028 if j else .04, (j-1.5)*.3)
    # Sparse changing phrases avoid an insistent repeated melody.
    if bar % 4 != 3:
        pitches = [chord[2]+12, chord[3]+12] if bar % 2 == 0 else [chord[1]+12]
        for j, pitch in enumerate(pitches):
            note(bar*6+1.7+j*2.1, pitch, 3.2, .018, (-1 if bar%2 else 1)*.25, .12)
save('GameOverAmbient', music)

for name, pitches in [('MenuSelect', [76,81]), ('MenuConfirm', [72,76,79])]:
    duration = .12 if name == 'MenuSelect' else .16
    sound = np.zeros((int(duration*RATE), 2))
    for j, midi in enumerate(pitches):
        start = int(j * .025 * RATE)
        t = np.arange(len(sound)-start) / RATE
        env = np.minimum(t/.004, 1) * np.maximum(0, 1-t/(duration-j*.025))**2
        signal = .12 * np.sin(2*np.pi*frequency(midi)*t)*env
        sound[start:] += signal[:, None]
    save(name, sound)
print('Created 96-second seamless ambience and menu sounds.')
