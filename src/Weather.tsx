import { memo, type CSSProperties } from 'react';
import peach from '../data/peach.json';

const rain = [29, 57, 105, 142, 187];
const petals = [
  { x: 159, y: 33, drift: 23, fall: 198, speed: 13, delay: .1, opacity: .64 },
  { x: 179, y: 44, drift: 9, fall: 184, speed: 16, delay: 3.1, opacity: .62 },
  { x: 15, y: 103, drift: 8, fall: 144, speed: 17, delay: 5.2, opacity: .5 }
];

const petalShape = 'M10 10 C8.7 9.2 5.45 7.5 5.55 4.75 C5.6 2.4 7.55 1.05 10 1.2 C12.45 1.05 14.4 2.4 14.45 4.75 C14.55 7.5 11.3 9.2 10 10 Z';
const budShape = 'M0 .5 Q-1.5 -.4 -1.1 -1.9 Q-.7 -3.1 .4 -2.6 Q1.6 -2.1 1 -1 Q.8 -.1 0 .5 Z';

function Blossom({ x, y, size, angle, shaded }: { x: number; y: number; size: number; angle: number; shaded: boolean }) {
  return (
    <g transform={`translate(${x} ${y}) rotate(${angle}) scale(${size}) translate(-10 -10)`}>
      {[0, 72, 144, 216, 288].map(turn => (
        <g key={turn} transform={`rotate(${turn} 10 10)`}>
          <path d={petalShape} fill={shaded ? 'url(#peach-shade)' : 'url(#peach-light)'} stroke="#c67899" strokeWidth=".38" />
          <path d="M10 9.8 Q9.4 8.4 9.7 7.2" fill="none" stroke="#b56386" strokeWidth=".42" strokeLinecap="round" />
        </g>
      ))}
      <circle cx="10" cy="10" r="1.35" fill="#a24d72" />
      <g fill="#fff0c9">
        <circle cx="10" cy="9.9" r=".55" /><circle cx="8.65" cy="9.2" r=".36" />
        <circle cx="11.3" cy="9.05" r=".34" /><circle cx="10.65" cy="11.3" r=".375" />
      </g>
    </g>
  );
}

export const Weather = memo(function Weather({ motion }: { motion: boolean }) {
  return (
    <div className={motion ? 'weather motion' : 'weather'} aria-hidden="true">
      {motion && rain.map((x, index) => (
        <i className="drop" key={x} style={{ left: x, animationDuration: `${5.8 + index * .62}s`, animationDelay: `${index * .77}s` }} />
      ))}
      {motion && petals.map(({ x, y, drift, fall, speed, delay, opacity }) => (
        <i className="petal" key={x} style={{ left: x, top: y, '--drift': `${drift}px`, '--fall': `${fall}px`,
          '--speed': `${speed}s`, '--delay': `${delay}s`, '--petal-alpha': opacity } as CSSProperties}>
          <svg viewBox="0 0 7 9"><path d="M1.5 2.5C4 0 7 2.7 6.5 5C6.1 6.8 4.6 8.1 3 8.8C3.3 6.2 .2 5 1.5 2.5Z" fill="url(#peach-falling)" /></svg>
        </i>
      ))}
      <svg className="branch" viewBox="0 0 147 61">
        <defs>
          <linearGradient id="peach-bark" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop stopColor="#533237" /><stop offset="1" stopColor="#29191e" />
          </linearGradient>
          <linearGradient id="peach-bud" x1="0%" y1="0%" x2="70%" y2="100%">
            <stop stopColor="#fbc4d7" /><stop offset="1" stopColor="#c56d96" />
          </linearGradient>
          <linearGradient id="peach-falling" x1="0%" y1="0%" x2="70%" y2="100%">
            <stop stopColor="#ffe1eb" /><stop offset="1" stopColor="#dc87aa" />
          </linearGradient>
          <linearGradient id="peach-light" x1="25%" y1="0%" x2="70%" y2="100%">
            <stop stopColor="#fff0f2" /><stop offset=".55" stopColor="#f8bdd1" /><stop offset="1" stopColor="#e58eaf" />
          </linearGradient>
          <linearGradient id="peach-shade" x1="25%" y1="0%" x2="70%" y2="100%">
            <stop stopColor="#ffe3ec" /><stop offset=".55" stopColor="#f3a8c4" /><stop offset="1" stopColor="#d978a0" />
          </linearGradient>
        </defs>
        {peach.Tips.map(({ X, Y, Strength, Delay, Stem, Leaves, Flowers, Buds }) => (
          <g className="peach-tip" key={X} style={{ transformOrigin: `${X}px ${Y}px`,
            '--breeze-angle': `${Strength}deg`, '--breeze-delay': `${Delay}s` } as CSSProperties}>
            <path d={Stem} fill="none" stroke="#ac7770" strokeWidth=".68" strokeLinecap="round" strokeLinejoin="round" />
            <path d={Leaves} fill="#41252c" />
            {Buds.map(([x, y, angle]) => (
              <path key={`${x},${y}`} d={budShape} fill="url(#peach-bud)" transform={`translate(${x} ${y}) rotate(${angle})`} />
            ))}
            {Flowers.map(([x, y, size, angle], index) => (
              <Blossom key={`${x},${y}`} x={x} y={y} size={size} angle={angle} shaded={index % 3 === 2} />
            ))}
          </g>
        ))}
        <path d={peach.Wood} fill="url(#peach-bark)" stroke="#7c514cb0" strokeWidth=".42" strokeLinejoin="round" />
        <path d={peach.Grain} fill="none" stroke="#9a6d5ebf" strokeWidth=".55" strokeLinecap="round" strokeLinejoin="round" />
        <path d={peach.Texture} fill="#79544eba" />
      </svg>
    </div>
  );
});
