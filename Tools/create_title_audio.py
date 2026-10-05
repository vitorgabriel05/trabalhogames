"""Original forest-adventure theme, 32 bars at 108 BPM; circular reverb for looping.
Requires numpy. Rebuilds only the new title assets; preserves existing death audio.
"""
from pathlib import Path
import wave
import numpy as np

RATE = 32000
BEAT = 60 / 108
LENGTH = 32 * 4 * BEAT
DEST = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
mix = np.zeros((round(LENGTH * RATE), 2), dtype=np.float64)
rng = np.random.default_rng(1948)

def add(at, pitch, beats, level, pan=0, kind='bell'):
    duration = beats * BEAT
    t = np.arange(round(duration * RATE)) / RATE
    f = 440 * 2 ** ((pitch - 69) / 12)
    if kind == 'pad':
        signal = np.sin(2*np.pi*f*t) + .16*np.sin(2*np.pi*(f*2+.3)*t)
        env = np.minimum(t/.16, 1)*np.minimum((duration-t)/.5, 1)
    elif kind == 'bass':
        signal = np.sin(2*np.pi*f*t)+.2*np.sin(2*np.pi*2*f*t)
        env = np.minimum(t/.012, 1)*np.exp(-t*2)*np.minimum((duration-t)/.08, 1)
    elif kind == 'flute':
        phase = 2*np.pi*f*t+.025*np.sin(2*np.pi*5*t)
        signal = np.sin(phase)+.09*np.sin(2*phase)
        env = np.minimum(t/.035,1)*np.minimum((duration-t)/.12,1)
    else:
        signal = np.sin(2*np.pi*f*t)+.24*np.sin(2*np.pi*3*f*t)*np.exp(-t*7)
        env = np.minimum(t/.004,1)*np.exp(-t*4)*np.minimum((duration-t)/.06,1)
    signal *= np.maximum(env,0)*level
    for delay,gain in [(0,1),(.19,.16),(.38,.07)]:
        idx=(round((at*BEAT+delay)*RATE)+np.arange(len(t)))%len(mix)
        mix[idx,0]+=signal*gain*np.sqrt((1-pan)/2)
        mix[idx,1]+=signal*gain*np.sqrt((1+pan)/2)

chords = [(48,52,55,59),(43,50,55,59),(45,52,57,60),(41,48,53,57),
          (48,52,55,60),(43,50,55,62),(41,48,53,57),(43,50,55,59)]
phrases = [[(0,76,1),(1.5,79,.5),(2,81,1),(3,79,.75)],
           [(0,74,1.5),(2,71,.75),(3,74,.75)],
           [(0,72,.75),(1,76,.75),(2,79,1.5)],
           [(0,77,1),(1.5,76,.5),(2,72,1.75)],
           [(0,79,1),(1,84,1),(2.5,83,.5),(3,79,.75)],
           [(0,81,1),(1.5,79,.5),(2,74,1.5)],
           [(0,77,1.5),(2,76,.75),(3,72,.75)],
           [(0,74,1),(1.5,71,.5),(2,67,1.5)]]
for bar in range(32):
    chord=chords[bar%8]
    for j,pitch in enumerate(chord): add(bar*4,pitch,4.7,.022,(j-1.5)*.3,'pad')
    for beat in [0,2]: add(bar*4+beat,chord[0]-12,1.4,.11,0,'bass')
    for step in range(8):
        add(bar*4+step*.5,chord[[0,2,1,3,2,1,3,2][step]]+12,.9,.035,(-.45 if step%2 else .45))
    # Four varied phrases: flute, bells, quieter middle, then the full theme.
    for at,pitch,duration in phrases[bar%8]:
        add(bar*4+at,pitch+(12 if 8<=bar<16 else 0),duration,.075 if bar<16 or bar>=24 else .047,
            .12,'bell' if 8<=bar<16 else 'flute')
    if bar>=8:
        for beat in [1,3]:
            t=np.arange(round(.09*RATE))/RATE
            noise=rng.normal(0,1,len(t)); noise=np.diff(noise,prepend=0)
            s=noise*np.exp(-t*65)*.013
            idx=(round((bar*4+beat)*BEAT*RATE)+np.arange(len(t)))%len(mix)
            mix[idx]+=s[:,None]

mix *= .65 / max(.001,np.max(np.abs(mix)))
DEST.mkdir(parents=True,exist_ok=True)
with wave.open(str(DEST/'TitleTheme.wav'),'wb') as out:
    out.setnchannels(2); out.setsampwidth(2); out.setframerate(RATE)
    out.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
print(f'TitleTheme: {LENGTH:.2f}s, stereo, 108 BPM, peak {np.max(np.abs(mix)):.3f}')

for name,pitches,duration in [('TitleSelect',[79,84],.10),('TitleConfirm',[72,76,79,84],.20)]:
    sound=np.zeros((round(duration*RATE),2))
    for j,pitch in enumerate(pitches):
        start=round(j*.024*RATE)
        t=np.arange(len(sound)-start)/RATE
        f=440*2**((pitch-69)/12)
        env=np.minimum(t/.003,1)*np.exp(-t*30)*np.minimum((duration-j*.024-t)/.02,1)
        tone=(np.sin(2*np.pi*f*t)+.16*np.sin(2*np.pi*2.7*f*t))*.19*np.maximum(env,0)
        sound[start:]+=tone[:,None]
    with wave.open(str(DEST/(name+'.wav')),'wb') as out:
        out.setnchannels(2); out.setsampwidth(2); out.setframerate(RATE)
        out.writeframes((sound*32767).astype('<i2').tobytes())
print('Created soft wooden selection and ascending confirmation sounds.')
