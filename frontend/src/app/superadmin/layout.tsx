'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { LayoutDashboard, Building2, TrendingUp, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

const NAV = [
  { href: '/superadmin/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/superadmin/orgs', label: 'Organizations', icon: Building2 },
  { href: '/superadmin/earnings', label: 'Earnings', icon: TrendingUp },
];

export default function SuperAdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const token = localStorage.getItem('saToken');
    if (!token && pathname !== '/superadmin/login') {
      router.replace('/superadmin/login');
    }
  }, [pathname, router]);

  if (!mounted) return null;

  if (pathname === '/superadmin/login') return <>{children}</>;

  const handleLogout = () => {
    localStorage.removeItem('saToken');
    localStorage.removeItem('saRefreshToken');
    router.replace('/superadmin/login');
  };

  return (
    <div className="flex min-h-screen bg-muted/30">
      <aside className="w-56 shrink-0 border-r bg-background flex flex-col">
        <div className="px-4 py-5 border-b">
          <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">Super Admin</p>
        </div>
        <nav className="flex-1 px-2 py-4 space-y-1">
          {NAV.map(({ href, label, icon: Icon }) => (
            <Link
              key={href}
              href={href}
              className={cn(
                'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                pathname.startsWith(href)
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-muted hover:text-foreground'
              )}
            >
              <Icon className="h-4 w-4" />
              {label}
            </Link>
          ))}
        </nav>
        <div className="px-2 py-4 border-t">
          <Button variant="ghost" size="sm" className="w-full justify-start gap-2 text-muted-foreground" onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </aside>
      <main className="flex-1 p-8 overflow-auto">{children}</main>
    </div>
  );
}
