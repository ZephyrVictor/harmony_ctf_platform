import { Button, Group, GroupProps, LoadingOverlay, Stack, Tabs } from '@mantine/core'
import {
  mdiAccountGroupOutline,
  mdiBugCheckOutline,
  mdiBullhornOutline,
  mdiCashMultiple,
  mdiChartBoxOutline,
  mdiClipboardListOutline,
  mdiCogSyncOutline,
  mdiDatabaseImportOutline,
  mdiFileDocumentCheckOutline,
  mdiFlagOutline,
  mdiGavel,
  mdiKeyboardBackspace,
  mdiTagOutline,
  mdiTextBoxOutline,
  mdiTicketOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, Link, useNavigate, useParams } from 'react-router'
import { AdminPage } from '@Components/admin/AdminPage'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import misc from '@Styles/Misc.module.css'

export interface GameEditTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
  contentPos?: React.CSSProperties['justifyContent']
  isLoading?: boolean
  backUrl?: string
}

export const WithGameEditTab: FC<GameEditTabProps> = ({
  children,
  isLoading,
  contentPos,
  head,
  backUrl,
  ...others
}) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id } = useParams()
  const { t } = useTranslation()

  const pages = [
    { icon: mdiTextBoxOutline, title: t('admin.tab.games.info'), path: 'info' },
    { icon: mdiBullhornOutline, title: t('admin.tab.games.notices'), path: 'notices' },
    { icon: mdiFlagOutline, title: t('admin.tab.games.challenges'), path: 'challenges' },
    { icon: mdiCogSyncOutline, title: t('admin.tab.games.submission_control', { defaultValue: '提交控制' }), path: 'submission-control' },
    { icon: mdiDatabaseImportOutline, title: t('admin.tab.games.import_flags', { defaultValue: 'Import Flags' }), path: 'import-flags' },
    { icon: mdiTagOutline, title: t('admin.tab.games.divisions'), path: 'divisions' },
    { icon: mdiAccountGroupOutline, title: t('admin.tab.games.review'), path: 'review' },
    { icon: mdiTicketOutline, title: t('admin.tab.games.tickets'), path: 'tickets' },
    { icon: mdiCashMultiple, title: t('admin.tab.games.score_adjustment', { defaultValue: '分数管理' }), path: 'score-adjustment' },
    { icon: mdiClipboardListOutline, title: t('admin.tab.games.score_ledger', { defaultValue: '调分记录' }), path: 'score-ledger' },
    { icon: mdiBugCheckOutline, title: t('admin.tab.games.zeroday_bonus_ledger', { defaultValue: '0day 加分记录' }), path: 'zeroday-bonus-ledger' },
    { icon: mdiChartBoxOutline, title: t('admin.tab.games.team_progress', { defaultValue: '队伍答题统计' }), path: 'team-progress' },
    { icon: mdiGavel, title: t('admin.tab.games.bans', { defaultValue: '封禁管理' }), path: 'bans' },
    { icon: mdiFileDocumentCheckOutline, title: t('admin.tab.games.writeups'), path: 'writeups' },
  ]

  const getTab = (path: string) => pages.find((page) => path.includes(page.path))

  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.path ?? pages[0].path)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.path ?? '')
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  return (
    <AdminPage
      {...others}
      head={
        <>
          <Button
            w="10rem"
            component={Link}
            classNames={{ inner: misc.justifyBetween }}
            leftSection={<Icon path={mdiKeyboardBackspace} size={1} />}
            to={backUrl ?? '/admin/games'}
          >
            {t('admin.button.back')}
          </Button>
          <Group wrap="nowrap" justify={contentPos ?? 'space-between'} w="calc(100% - 11rem)">
            {head}
          </Group>
        </>
      }
    >
      <Group wrap="nowrap" justify="space-between" align="flex-start" w="100%" pb="xl">
        <Tabs
          orientation="vertical"
          value={activeTab}
          onChange={(value) => value && navigate(`/admin/games/${id}/${value}`)}
          classNames={{
            root: misc.w10rem,
            list: misc.w10rem,
          }}
        >
          <Tabs.List>
            {pages.map((page) => (
              <Tabs.Tab key={page.path} leftSection={<Icon path={page.icon} size={1} />} value={page.path}>
                {page.title}
              </Tabs.Tab>
            ))}
          </Tabs.List>
        </Tabs>
        <Stack w="calc(100% - 11rem)" pos="relative">
          <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />
          {children}
        </Stack>
      </Group>
    </AdminPage>
  )
}
