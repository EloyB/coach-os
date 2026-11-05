import { PlayerErasureForm } from './player-erasure-form';

export default async function PrivacyPage({ searchParams }: { searchParams: Promise<{ id?: string; kind?: string }> }) {
  const params = await searchParams;
  return <PlayerErasureForm key={`${params.kind}:${params.id}`} initialId={params.id ?? ''} initialKind={params.kind === 'camp-enrollments' ? 'camp-enrollments' : 'enrollments'} />;
}
