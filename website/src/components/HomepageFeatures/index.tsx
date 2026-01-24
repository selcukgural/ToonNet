import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: '⚡ High Performance',
    icon: '🚀',
    description: (
      <>
        Expression tree-based serialization with <strong>zero reflection overhead</strong>.
        10-100x faster than traditional reflection-based serializers with optimized
        memory usage and object pooling.
      </>
    ),
  },
  {
    title: '🤖 AI Token Optimized',
    icon: '💰',
    description: (
      <>
        TOON format uses <strong>up to 40% fewer tokens</strong> than JSON, reducing AI API costs.
        Perfect for LLM prompts, AI training data, and agent memory.
        Human-readable and machine-friendly.
      </>
    ),
  },
  {
    title: '🔧 Developer Friendly',
    icon: '💻',
    description: (
      <>
        <strong>System.Text.Json-compatible API</strong> for zero learning curve.
        Full .NET 8+ support with nullable reference types.
        Comprehensive documentation and 444+ passing tests.
      </>
    ),
  },
];

function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center" style={{fontSize: '4rem', marginBottom: '1rem'}}>
        {icon}
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
