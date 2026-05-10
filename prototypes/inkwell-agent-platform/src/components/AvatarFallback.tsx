import { Avatar } from 'antd';

const COLORS = [
  '#7265e6',
  '#ffbf00',
  '#00a2ae',
  '#1677ff',
  '#fa541c',
  '#13c2c2',
  '#52c41a',
  '#eb2f96'
];

function hashColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) | 0;
  return COLORS[Math.abs(h) % COLORS.length];
}

interface Props {
  name: string;
  size?: number;
}

/**
 * 头像 fallback：未上传时按字符哈希分配背景色 + 取首字符（OQ-019 closed 2026-05-08）
 */
export default function AvatarFallback({ name, size = 32 }: Props) {
  const ch = (name || '?').trim().charAt(0).toUpperCase();
  return (
    <Avatar size={size} style={{ background: hashColor(name || '?') }}>
      {ch}
    </Avatar>
  );
}
