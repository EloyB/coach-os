import { Fragment } from 'react';
import { AbsoluteFill } from 'remotion';
import { Stage } from './Stage';
import { Headline, SceneTag } from './KineticText';
import { LAYOUTS, type PromoFormat } from './layout';

// Gedeelde scène-opbouw voor de vier feature-scènes:
// 16:9 → headline links, demo rechts. 9:16 → headline boven, demo eronder.

export const SceneShell: React.FC<{
  format: PromoFormat;
  tag: string;
  headline: string[];
  headlineAt?: number;
  children: React.ReactNode;
}> = ({ format, tag, headline, headlineAt = 6, children }) => {
  const L = LAYOUTS[format];
  const isRow = L.direction === 'row';
  return (
    <Stage format={format}>
      <AbsoluteFill
        style={{
          padding: L.pad,
          display: 'flex',
          flexDirection: L.direction,
          alignItems: 'center',
          justifyContent: isRow ? 'space-between' : 'flex-start',
          gap: isRow ? 60 : 54,
        }}
      >
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            gap: 26,
            flexShrink: 0,
            width: isRow ? L.headline.maxWidth : '100%',
            marginTop: isRow ? 0 : 30,
          }}
        >
          <SceneTag text={tag} at={2} fontSize={L.tagFontSize} />
          <Headline
            lines={headline}
            at={headlineAt}
            fontSize={L.headline.fontSize}
            lineHeight={L.headline.lineHeight}
            maxWidth={L.headline.maxWidth}
          />
        </div>
        <div
          style={{
            flex: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            transform: `scale(${L.cardScale})`,
            minHeight: 0,
          }}
        >
          <Fragment>{children}</Fragment>
        </div>
      </AbsoluteFill>
    </Stage>
  );
};
