'use client';

import { useState, useSyncExternalStore } from 'react';
import { useTranslations } from 'next-intl';
import { useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { getAuthUser } from '@/lib/auth';
import { processPlayerErasure, type ErasureKind, type ErasureResult } from '@/lib/api/privacy';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';

const subscribe = () => () => {};
export function PlayerErasureForm({ initialId, initialKind }: { initialId: string; initialKind: ErasureKind }) {
  const t = useTranslations('playerErasure');
  const queryClient = useQueryClient();
  const admin = useSyncExternalStore(subscribe, () => getAuthUser()?.role === 'Admin' && !getAuthUser()?.isSuperAdmin, () => false);
  const [id, setId] = useState(initialId);
  const [kind, setKind] = useState<ErasureKind>(initialKind);
  const [early, setEarly] = useState(false);
  const [leader, setLeader] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [verified, setVerified] = useState(false);
  const [result, setResult] = useState<ErasureResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  function invalidate() { setResult(null); setVerified(false); setConfirmation(''); setError(false); }
  async function run(apply: boolean) {
    if (busy || !admin) return;
    setBusy(true); setError(false);
    try {
      const next = await processPlayerErasure(kind, id, {
        early, newGroupLeaderId: leader || null,
        ...(apply ? { apply: true, previewHash: result?.previewHash ?? undefined, confirmation, verifiedRequest: verified } : {}),
      });
      setResult(next);
      if (next.deleted) await queryClient.invalidateQueries();
      else if (apply) { setVerified(false); setConfirmation(''); setError(true); }
    } catch { setResult(null); setError(true); }
    finally { setBusy(false); }
  }
  const valid = z.uuid().safeParse(id).success && (!leader || z.uuid().safeParse(leader).success);
  return <div className="max-w-2xl space-y-5">
    <h1 className="text-2xl font-semibold text-ink">{t('title')}</h1>
    <p className="text-sm text-muted-foreground">{t('description')}</p>
    {!admin ? <p role="alert">{t('adminOnly')}</p> : <Card>
      <CardHeader><CardTitle>{t('scope')}</CardTitle></CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2"><Label htmlFor="erasure-kind">{t('kind')}</Label><select id="erasure-kind" className="h-9 w-full rounded-md border border-input bg-paper px-3 text-sm" value={kind} disabled={busy || result?.deleted} onChange={e => { setKind(e.target.value as ErasureKind); invalidate(); }}><option value="enrollments">{t('lesson')}</option><option value="camp-enrollments">{t('camp')}</option></select></div>
        <div className="space-y-2"><Label htmlFor="erasure-id">{t('id')}</Label><Input id="erasure-id" value={id} disabled={busy || result?.deleted} onChange={e => { setId(e.target.value); invalidate(); }} /></div>
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={early} disabled={busy || result?.deleted} onChange={e => { setEarly(e.target.checked); invalidate(); }} />{t('early')}</label>
        <div className="space-y-2"><Label htmlFor="erasure-leader">{t('leader')}</Label><Input id="erasure-leader" value={leader} disabled={busy || result?.deleted} onChange={e => { setLeader(e.target.value); invalidate(); }} /><p className="text-xs text-muted-foreground">{t('leaderHelp')}</p></div>
        {!result?.deleted && <Button disabled={busy || !valid} onClick={() => run(false)}>{busy ? t('busy') : t('preview')}</Button>}
        {error && <p role="alert" className="text-sm text-destructive">{t('error')}</p>}
        {result?.deleted ? <p role="status" className="font-medium text-tennis-green">{t('done')}</p> : result && <section className="space-y-4 border-t border-rule pt-4" aria-live="polite">
          <p className="font-medium">{t('records', { count: result.personalRecords })}</p>
          <p className="text-sm text-muted-foreground">{t('effect')}</p>
          {result.holds.length > 0 && <ul className="list-inside list-disc text-sm">{result.holds.map(hold => <li key={hold}>{t.has(`holds.${hold}`) ? t(`holds.${hold}`) : t('unknownHold')}</li>)}</ul>}
          {result.eligible && result.previewHash && <>
            <div className="space-y-2"><Label htmlFor="erasure-confirmation">{t('name')}</Label><Input id="erasure-confirmation" maxLength={250} value={confirmation} disabled={busy} onChange={e => setConfirmation(e.target.value)} /></div>
            <label className="flex items-start gap-2 text-sm"><input type="checkbox" checked={verified} disabled={busy} onChange={e => setVerified(e.target.checked)} />{t('verified')}</label>
            <Button variant="destructive" disabled={busy || !verified || !confirmation.trim()} onClick={() => run(true)}>{busy ? t('busy') : t('apply')}</Button>
          </>}
        </section>}
      </CardContent>
    </Card>}
  </div>;
}
