'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import api from '@/lib/api';
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';

type Status = 'verifying' | 'success' | 'error';

export default function VerifyEmailPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const [status, setStatus] = useState<Status>('verifying');
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('No verification token provided.');
      return;
    }

    api
      .post('/auth/verify-email', { token })
      .then(() => setStatus('success'))
      .catch((err) => {
        setStatus('error');
        setMessage(
          err?.response?.data?.error ?? 'Verification failed. The link may have expired.'
        );
      });
  }, [token]);

  return (
    <Card>
      <CardHeader>
        {status === 'verifying' && (
          <>
            <CardTitle>Verifying your email…</CardTitle>
            <CardDescription>Please wait a moment.</CardDescription>
          </>
        )}
        {status === 'success' && (
          <>
            <CardTitle>Email verified</CardTitle>
            <CardDescription>
              Your email has been verified. You can now sign in.
            </CardDescription>
          </>
        )}
        {status === 'error' && (
          <>
            <CardTitle>Verification failed</CardTitle>
            <CardDescription className="text-destructive">
              {message || 'Something went wrong. Please try again.'}
            </CardDescription>
          </>
        )}
      </CardHeader>
      {status !== 'verifying' && (
        <CardFooter>
          <Link href="/login" className="text-sm text-primary hover:underline">
            Go to login
          </Link>
        </CardFooter>
      )}
    </Card>
  );
}
