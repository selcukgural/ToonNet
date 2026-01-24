import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: '⚡ High Performance',
    Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        Expression tree-based serialization with zero reflection overhead.
        10-100x faster than traditional reflection-based serializers.
        Object pooling and optimized memory usage.
      </>
    ),
  },
  {
    title: '🤖 AI Token Optimized',
    Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        TOON format uses up to 40% fewer tokens than JSON, reducing AI API costs.
        Perfect for LLM prompts, AI training data, and agent memory.
        Human-readable and machine-friendly.
      </>
    ),
  },
  {
    title: '🔧 Developer Friendly',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        System.Text.Json-compatible API for zero learning curve.
        Full .NET 8+ support with nullable reference types.
        Comprehensive documentation and 444+ passing tests.
      </>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
