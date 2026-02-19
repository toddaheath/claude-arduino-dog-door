interface SkeletonProps {
  width?: string | number;
  height?: string | number;
}

export function Skeleton({ width = '100%', height = 16 }: SkeletonProps) {
  return (
    <div
      className="skeleton"
      style={{
        width,
        height: typeof height === 'number' ? `${height}px` : height,
      }}
    />
  );
}

export function SkeletonCard() {
  return (
    <div className="card" style={{ gap: 8 }}>
      <div className="skeleton skeleton--card" />
      <div className="skeleton skeleton--text" style={{ width: '60%' }} />
      <div className="skeleton skeleton--text" style={{ width: '40%', height: '12px' }} />
    </div>
  );
}

export function SkeletonRow() {
  return (
    <tr>
      {[80, 60, 40, 60, 40, 50].map((w, i) => (
        <td key={i} style={{ padding: 8 }}>
          <div className="skeleton skeleton--text" style={{ width: `${w}%` }} />
        </td>
      ))}
    </tr>
  );
}
